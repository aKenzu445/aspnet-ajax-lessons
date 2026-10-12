# Ajax часть 2 — поиск фильмов через OMDB API

**Сайт онлайн:** https://akenzu445.github.io/aspnet-ajax-lessons/OmdbSearch/

Веб-страница для поиска фильмов. Все запросы к серверу [OMDB](https://www.omdbapi.com/) выполняются через **AJAX** (`fetch`), страница ни разу не перезагружается.

## Файлы

| Файл | Что внутри |
|---|---|
| `index.html` | Разметка: форма поиска (Title, Type, Search) и пустые блоки `#films`, `#pagination`, `#details` |
| `style.css` | Оформление: форма, карточки фильмов, кнопки страниц, таблица подробностей |
| `app.js` | Вся логика на JavaScript |
| `config.js` | Ключ OMDB API (`OMDB_API_KEY`) |
| `config.example.js` | Шаблон `config.js` без настоящего ключа |

## Как выполнено задание (по шагам в `app.js`)

| Шаг | Требование задания | Как сделано |
|---|---|---|
| 1 | Форма: название + тип (movie / series / episode), кнопка Search | Обработчик `click` на кнопке берёт `.value` полей и вызывает `searchFilms(1)` |
| 2 | Запрос к OMDB через AJAX, список фильмов под формой, сообщение **Movie not found!** | `searchFilms(page)`: `fetch` по адресу `?s=название&type=тип&page=N`, затем `response.json()`, затем цикл `for...of` по `data.Search` создаёт карточки. Если `data.Response === "False"`, выводится «Movie not found!» |
| 3 | Пагинация: OMDB отдаёт по 10 фильмов, нужны кнопки страниц | `totalPages = Math.ceil(Number(data.totalResults) / 10)`. `renderPagination()` рисует `<<`, номера страниц (окно из 7 штук), `>>`. Клик вызывает `searchFilms(номер)` |
| 4 | Кнопка **Details** показывает подробности под списком и пагинацией | `showDetails(imdbID)`: `fetch` по адресу `?i=imdbID`, результат выводится таблицей (Title, Released, Genre, Country, Director, Writer, Actors, Awards) |

Дополнительно: сообщение «Загрузка...», обработка ошибки сети (`try/catch`), заглушка, если у фильма нет постера (`"N/A"`), поиск по клавише Enter.

## Как запустить у себя

Двойной клик по `index.html`, страница откроется в браузере. Или в Rider: правый клик по `index.html` → **Open In → Browser**.
