# Занятие 6. CRUD: реализация Update (тег-хелперы, привязка модели)

Продолжение проекта **Lesson1.WebServer**. Добавлена недостающая операция **Update**: страница редактирования `/Tasks/Edit/{id}` и отметка «выполнено» через AJAX.
Выводы и проверки — в [`notes.md`](notes.md).

## Как запустить
Rider → `AjaxLessons.slnx` → вверху справа конфигурация **Занятие 6** → ▶ (или правый клик по проекту **Lesson6.WebServer** → **Run**). Адрес: **http://localhost:5106**. База `tasks.db` создаётся автоматически.

## Новое в проекте

| Файл | Что внутри |
|---|---|
| `Pages/Tasks/Edit.cshtml` | Страница редактирования: `@page "{id:int}"`, форма на тег-хелперах `asp-for`, `asp-validation-for`, `<select>` категорий |
| `Pages/Tasks/Edit.cshtml.cs` | `EditModel`: `[BindProperty] TaskItem Task`, `OnGetAsync(int id)`, `OnPostAsync()` |
| `Models/TaskItem.cs` | `[Display]`, `[Required]`, `[StringLength(100)]` на Title; `[AllowedCategory]` на Category |
| `Models/TaskCategories.cs` | ДЗ: список допустимых категорий |
| `Validation/AllowedCategoryAttribute.cs` | ДЗ: собственный атрибут валидации |
| `Migrations/..._TitleMaxLength.cs` | Модель EF учла `[StringLength(100)]` |

## CRUD — где что

| Операция | AJAX (API) | Классическая форма (Razor Pages) |
|---|---|---|
| Create | `POST /api/tasks` | `/Tasks` → `OnPostAsync` (`[BindProperty] NewTask`) |
| Read | `GET /api/tasks`, `GET /api/tasks/{id}` | `/Tasks` → `OnGetAsync` |
| **Update** | `PUT /api/tasks/{id}` (чекбокс `IsDone`) | **`/Tasks/Edit/{id}` → `OnPostAsync`** (название, статус, категория) |
| Delete | `DELETE /api/tasks/{id}` | `/Tasks` → `OnPostDeleteAsync` |

## Задачи

| № | Задача | Где | Как проверить |
|---|---|---|---|
| 1 | Нет операции Update | `notes.md` | Таблица CRUD «до» |
| 2 | Страница с параметром маршрута | `Pages/Tasks/Edit.cshtml` (`@page "{id:int}"`) | `/Tasks/Edit/3` → «Редактирование задачи №3», `abc` → 404 |
| 3 | `[BindProperty]` и загрузка | `EditModel.OnGetAsync` | Данные задачи на странице |
| 4 | Форма на тег-хелперах | `Edit.cshtml` | View Source: `value` и `checked` подставлены автоматически |
| 5 | Сохранение | `EditModel.OnPostAsync` | Изменить название → «Задача обновлена!» → новое название в списке |
| 6 | Ссылка «Редактировать» | `_TaskItem.cshtml` (`asp-page`, `asp-route-id`), JS-шаблон | Ведёт на `/Tasks/Edit/{id}` нужной задачи |
| 7 | DataAnnotations + `ModelState` | `TaskItem.Title`, `OnPostAsync` | Пустое название → «Название обязательно», не сохранено |
| 8 | Форма добавления на `[BindProperty]` | `TasksModel.NewTask`, `Tasks.cshtml` | Пустое название → та же ошибка, что при редактировании |
| 9 | Чекбокс через AJAX PUT | `toggleTask()` в `Tasks.cshtml`, `PUT /api/tasks/{id}` | Клик → текст зачёркнут без перезагрузки |
| 10 | «Протухшая» ссылка | `EditModel.OnGetAsync` | `/Tasks/Edit/999` → редирект и «Задача не найдена, возможно, она уже удалена» |
| ДЗ | Категория `<select>` + свой атрибут | `Edit.cshtml`, `AllowedCategoryAttribute` | Недопустимая категория → ошибка валидации |

## Финальные тесты (задача 11, проверено ✅)

| № | Действие | Результат |
|---|---|---|
| 1 | Добавить задачу классической формой | Сохраняется с валидацией, тег-хелперы работают ✅ |
| 2 | Добавить через AJAX-форму | Мгновенно в списке ✅ |
| 3 | Открыть `/Tasks/Edit/{id}` | Форма предзаполнена через `asp-for` ✅ |
| 4 | Изменить название и сохранить | Редирект, «Задача обновлена!», новое название ✅ |
| 5 | Сохранить пустое название | Ошибка валидации, изменения не применены ✅ |
| 6 | Чекбокс в списке | Мгновенно через AJAX PUT ✅ |
| 7 | Удалить (AJAX и формой) | Оба способа работают ✅ |
| 8 | `/Tasks/Edit/{несуществующий}` | Редирект с сообщением, без 500 ✅ |
