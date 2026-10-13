// === зан9 зад11: одна обертка на все ajax запросы к api ===
// раньше в каждом обработчике одно и то же: fetch -> проверка response.ok -> разбор ошибки -> показ -> try/catch
// теперь всё тут. подключено в _Layout.cshtml, так что есть на всех страницах

// зан2 зад7: ключ для запросов которые меняют /api/tasks (без него 401)
// let а не const - для проверки дз можно в консоли написать API_KEY = "wrong"
let API_KEY = "lesson2-secret";

// url - адрес api, типа "/api/tasks/5"
// options - как у fetch: { method, body, signal }. body можно кинуть объектом, сам станет json
// возвращает данные ответа (или null для 204 / отмены)
async function apiRequest(url, options = {}) {
    const method = (options.method ?? "GET").toUpperCase();
    const headers = { ...(options.headers ?? {}) };   // ... - копия заголовков
    let body = options.body;

    if (body !== undefined && typeof body !== "string") {
        body = JSON.stringify(body);                     // объект -> строка json
        headers["Content-Type"] = "application/json";
    }
    if (method !== "GET") {
        headers["X-Api-Key"] = API_KEY;                  // запросам которые меняют данные нужен ключ
    }

    try {
        const response = await fetch(url, { ...options, method, headers, body });

        // у 204 тела нет - .catch(() => null) чтоб не было ошибки разбора json
        const data = await response.json().catch(() => null);

        if (!response.ok) {
            // зан9 зад6: общий формат { error: { message, code } }
            const error = new Error(data?.error?.message || "Неизвестная ошибка сервера");
            error.code = data?.error?.code;      // код ошибки для программы
            error.status = response.status;      // http код (400, 404, ...)
            error.field = data?.error?.field;    // поле формы с ошибкой (дз)
            throw error;
        }

        return data;
    } catch (err) {
        if (err.name === "AbortError") {
            return null;                         // запрос специально отменили (живой поиск)
        }
        if (err.status === undefined) {
            err.message = `Ошибка сети: ${err.message}`;   // сервак недоступен / нет инета
        }
        reactToErrorCode(err);
        throw err;                               // чтоб вызывающий код тоже мог среагировать
    }
}

// === дз зан9: разное поведение по коду ошибки ===
// switch выбирает ветку по err.code (коды в Api/ApiError.cs на серваке)
function reactToErrorCode(err) {
    switch (err.code) {
        case "UNAUTHORIZED":
            // отдельное сообщение: дело не в данных, а в ключе
            showErrorToast("Нет доступа: нужен правильный API-ключ в заголовке X-Api-Key (см. занятие 2). " +
                           "Проверьте значение API_KEY в wwwroot/js/api.js.");
            break;
        case "VALIDATION_ERROR":
            // подсвечиваю поле на которое указал сервак (error.field)
            highlightField(err.field);
            showErrorToast(err.message);
            break;
        case "TOO_MANY_REQUESTS":
            showErrorToast("Слишком много запросов. Подождите несколько секунд и повторите.");
            break;
        case "MAINTENANCE":
            showErrorToast("На сервере технические работы. Попробуйте позже.");
            break;
        default:
            showErrorToast(err.message);           // остальные ошибки - как есть
    }
}

// поля форм помечены data-api-field="имя поля json" (title, categoryId, dueDate)
// подсветка снимается как тока начинаешь исправлять
function highlightField(field) {
    if (!field) {
        return;
    }
    document.querySelectorAll(`[data-api-field="${field}"]`).forEach(element => {
        element.classList.add("api-field-error");
        element.focus();
        element.addEventListener("input", () => element.classList.remove("api-field-error"), { once: true });
    });
}

// всплывашка с ошибкой справа снизу (пропадает через 4 сек)
function showErrorToast(message) {
    const toast = document.createElement("div");
    toast.className = "toast-error";
    toast.textContent = message;               // textContent - без риска html-инъекции
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 4000);
}
