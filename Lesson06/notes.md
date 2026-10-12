# Занятие 6 — заметки и выводы

## Задача 1. Обнаружение недостающей функции (постановка задачи)

**Баг-репорт:** «Создал задачу с опечаткой "Изучитьт ASP.NET" — исправить нельзя, только удалить и создать заново. Это не CRUD, а CRD!»

**Проверка проекта** (все endpoints и обработчики страниц):

| Операция CRUD | AJAX / API | Классические формы (Razor Pages) |
|---|---|---|
| **C**reate | `POST /api/tasks` | `TasksModel.OnPostAsync` |
| **R**ead | `GET /api/tasks`, `GET /api/tasks/{id}` | `TasksModel.OnGetAsync`, `StatsModel.OnGetAsync` |
| **U**pdate | только `PUT /api/tasks/{id}` — меняет **один флаг `IsDone`** (ДЗ занятия 1) | **нет ничего** |
| **D**elete | `DELETE /api/tasks/{id}` | `TasksModel.OnPostDeleteAsync` |

**Вывод (постановка задачи на занятие):** в проекте нет способа изменить **название** уже созданной задачи. Техническая причина: нет ни endpoint'а, ни `OnPost`-обработчика, который принимал бы новые значения полей и сохранял их в базе (`UPDATE`). Существующий `PUT` принимает только `{ "isDone": ... }`.

**Нужно сделать:** страницу редактирования `/Tasks/Edit/{id}` с формой на тег-хелперах (`asp-for`), привязкой модели (`[BindProperty]`) и обработчиком `OnPostAsync`, который сохраняет изменения. Плюс отметку «выполнено» одним кликом через AJAX `PUT`.

## Задачи 2–4. Страница редактирования, привязка модели, тег-хелперы

**Маршрут (задача 2):** `@page "{id:int}"` в `Pages/Tasks/Edit.cshtml` → адрес `/Tasks/Edit/{id}`.
```
/Tasks/Edit/3   → 200, заголовок «Редактирование задачи №3»
/Tasks/Edit/abc → 404 (route constraint :int, как в занятии 2)
```

**Загрузка (задача 3):** `EditModel.OnGetAsync(int id)` → `Task = await _db.Tasks.FindAsync(id)`.
`/Tasks/Edit/1` → «Изучить ASP.NET / False».

**Сгенерированный HTML формы (задача 4, «Просмотр кода страницы»):**
```html
<input type="hidden" ... id="Task_Id" name="Task.Id" value="1" />
<label for="Task_Title">Название</label>
<input type="text" ... id="Task_Title" name="Task.Title" value="Изучить ASP.NET" />
<input type="checkbox" checked="checked" ... id="Task_IsDone" name="Task.IsDone" value="true" />
<input name="__RequestVerificationToken" type="hidden" value="CfDJ8..." />
<input name="Task.IsDone" type="hidden" value="false" />
```
Что сделали тег-хелперы **сами**, без единой ручной строки:
- `name`/`id` из пути к свойству (`Task.Title` → `name="Task.Title"`, `id="Task_Title"`);
- `value` с текущим значением из модели, `checked` для `IsDone == true`;
- `<label>` с текстом из `[Display(Name = "Название")]`;
- скрытое `Task.IsDone=false`: если галочку снять, браузер не отправляет чекбокс вообще, а это поле передаст `false`;
- `data-val-*` атрибуты для клиентской валидации (пригодятся в занятии 7);
- анти-подделочный токен `__RequestVerificationToken`.
