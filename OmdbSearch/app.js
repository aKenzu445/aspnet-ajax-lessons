// адрес api, ключ OMDB_API_KEY лежит в config.js
const API_URL = "https://www.omdbapi.com/";

// сколько фильмов OMDB отдает за раз (поменять нельзя, так api устроено)
const PAGE_SIZE = 10;

// сколько кнопок с номерами показывать сразу
const MAX_PAGE_BUTTONS = 7;

// что ищем щас - запоминаю, чтоб при переходе на другую страницу кинуть тот же запрос с другим номером
let currentTitle = "";
let currentType = "";

// блоки куда js пихает результаты
const filmsBlock = document.getElementById("films");
const paginationBlock = document.getElementById("pagination");
const detailsBlock = document.getElementById("details");


// === шаг 1: клик по Search ===
document.getElementById("searchBtn").addEventListener("click", () => {
    currentTitle = document.getElementById("title").value.trim();
    currentType = document.getElementById("type").value;

    if (currentTitle === "") {
        filmsBlock.innerHTML = `<p class="not-found">Введите название фильма</p>`;
        paginationBlock.innerHTML = "";
        detailsBlock.innerHTML = "";
        return;
    }

    searchFilms(1); // новый поиск всегда с первой страницы
});

// поиск еще и по Enter в поле Title
document.getElementById("title").addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
        document.getElementById("searchBtn").click();
    }
});


// === шаг 2: запрос к OMDB и карточки ===
// отдельная функция, потому что её зовет и кнопка Search (стр 1), и кнопки страниц
async function searchFilms(page) {
    const url = `${API_URL}?apikey=${OMDB_API_KEY}&s=${encodeURIComponent(currentTitle)}&type=${currentType}&page=${page}`;

    detailsBlock.innerHTML = "";                       // старые подробности уже не нужны
    filmsBlock.innerHTML = `<p>Загрузка...</p>`;

    let data;
    try {
        const response = await fetch(url);             // ajax запрос, страница не перезагружается
        data = await response.json();                  // текст json -> объект js
    } catch (error) {
        // сюда попадаем если нет инета или OMDB лежит
        filmsBlock.innerHTML = `<p class="not-found">Ошибка сети: ${error.message}</p>`;
        paginationBlock.innerHTML = "";
        return;
    }

    // ничего не нашли - пишу и выхожу
    if (data.Response === "False") {
        filmsBlock.innerHTML = `<p class="not-found">Movie not found!</p>`;
        paginationBlock.innerHTML = "";
        return;
    }

    filmsBlock.innerHTML = "";   // убираю старые результаты

    // на каждый фильм карточка
    for (const film of data.Search) {
        filmsBlock.innerHTML += `
            <div class="film-card">
                <img src="${posterUrl(film.Poster)}" alt="Постер">
                <div>
                    <div class="film-type">${film.Type}</div>
                    <b>${film.Title}</b>
                    <div>${film.Year}</div>
                    <button onclick="showDetails('${film.imdbID}')">Details</button>
                </div>
            </div>`;
    }

    // totalResults приходит строкой ("875"), делаю число
    const totalPages = Math.ceil(Number(data.totalResults) / PAGE_SIZE);
    renderPagination(page, totalPages);
}

// у некоторых фильмов нет постера - OMDB шлет "N/A"
function posterUrl(poster) {
    if (poster === "N/A") {
        return "https://placehold.co/300x445?text=No+poster";
    }
    return poster;
}


// === шаг 3: кнопки страниц << 1 2 3 ... >> ===
function renderPagination(currentPage, totalPages) {
    paginationBlock.innerHTML = "";

    if (totalPages <= 1) {
        return; // всего одна страница - кнопки не нужны
    }

    // показываю "окно" из MAX_PAGE_BUTTONS номеров вокруг текущей, а то при 88 страницах было бы 88 кнопок
    let start = Math.max(1, currentPage - Math.floor(MAX_PAGE_BUTTONS / 2));
    const end = Math.min(totalPages, start + MAX_PAGE_BUTTONS - 1);
    start = Math.max(1, end - MAX_PAGE_BUTTONS + 1);

    // кнопка назад
    if (currentPage > 1) {
        paginationBlock.innerHTML += `<button onclick="searchFilms(${currentPage - 1})">&lt;&lt;</button>`;
    }

    // кнопки с номерами, текущая с классом active
    for (let page = start; page <= end; page++) {
        const activeClass = page === currentPage ? "active" : "";
        paginationBlock.innerHTML += `<button class="${activeClass}" onclick="searchFilms(${page})">${page}</button>`;
    }

    // кнопка вперед
    if (currentPage < totalPages) {
        paginationBlock.innerHTML += `<button onclick="searchFilms(${currentPage + 1})">&gt;&gt;</button>`;
    }
}


// === шаг 4: подробности фильма (Details) ===
async function showDetails(imdbID) {
    // i= - запрос ОДНОГО фильма по id (а s= - поиск списка)
    const url = `${API_URL}?apikey=${OMDB_API_KEY}&i=${imdbID}`;

    detailsBlock.innerHTML = `<p>Загрузка...</p>`;

    let film;
    try {
        const response = await fetch(url);
        film = await response.json();
    } catch (error) {
        detailsBlock.innerHTML = `<p class="not-found">Ошибка сети: ${error.message}</p>`;
        return;
    }

    if (film.Response === "False") {
        detailsBlock.innerHTML = `<p class="not-found">${film.Error}</p>`;
        return;
    }

    detailsBlock.innerHTML = `
        <h2>Film info:</h2>
        <div class="details-body">
            <img src="${posterUrl(film.Poster)}" alt="Постер">
            <table>
                <tr><td>Title:</td><td>${film.Title}</td></tr>
                <tr><td>Released:</td><td>${film.Released}</td></tr>
                <tr><td>Genre:</td><td>${film.Genre}</td></tr>
                <tr><td>Country:</td><td>${film.Country}</td></tr>
                <tr><td>Director:</td><td>${film.Director}</td></tr>
                <tr><td>Writer:</td><td>${film.Writer}</td></tr>
                <tr><td>Actors:</td><td>${film.Actors}</td></tr>
                <tr><td>Awards:</td><td>${film.Awards}</td></tr>
            </table>
        </div>`;

    // плавно скроллю к подробностям
    detailsBlock.scrollIntoView({ behavior: "smooth" });
}
