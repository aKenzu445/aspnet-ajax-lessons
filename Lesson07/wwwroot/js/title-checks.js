// зан7: проверки названия в браузере, которые unobtrusive сама не умеет
// это тока для удобства (ошибка сразу), сервак всё равно проверит еще раз

// === зад6: не пускаю слово "тест" в обычной форме (до отправки) ===
// form - сама форма, input - поле названия
function attachForbiddenWordsCheck(form, input) {
    // span куда asp-validation-for пишет ошибки этого поля
    const errorSpan = form.querySelector(`[data-valmsg-for="${input.name}"]`);

    form.addEventListener("submit", (event) => {
        if (input.value.toLowerCase().includes("тест")) {
            event.preventDefault();   // ОТМЕНА отправки формы - запрос на сервак не уйдет

            // ошибку показываю в следующем "такте" (setTimeout 0): сразу после моего обработчика срабатывает jquery.validate,
            // по ЕГО правилам поле норм, и он бы стер span
            setTimeout(() => {
                errorSpan.textContent = "Название не должно содержать слово 'тест'";
                errorSpan.className = "field-error field-validation-error";
                input.classList.add("input-validation-error");
            }, 0);
        }
    });
}
