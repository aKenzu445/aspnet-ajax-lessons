// адрес api, ключ OMDB_API_KEY лежит в config.js
const API_URL = "https://www.omdbapi.com/";

// ===== ШАГ 1–2: при клике на Search отправить запрос и показать карточки фильмов =====

document.getElementById("searchBtn").addEventListener("click", async () => {
    const title = document.getElementById("title").value;
    const type = document.getElementById("type").value;

    const url = `${API_URL}?apikey=${OMDB_API_KEY}&s=${encodeURIComponent(title)}&type=${type}&page=1`;

    const response = await fetch(url);
    const data = await response.json();

    const filmsBlock = document.getElementById("films");

    // ничего не нашли - пишу и выхожу
    if (data.Response === "False") {
        filmsBlock.innerHTML = `<p class="not-found">Movie not found!</p>`;
        return;
    }

    filmsBlock.innerHTML = "";   // убираю старые результаты

    // на каждый фильм карточка
    for (const film of data.Search) {
        filmsBlock.innerHTML += `
            <div class="film-card">
                <img src="${film.Poster}" alt="Постер">
                <div>
                    <div>${film.Type}</div>
                    <b>${film.Title}</b>
                    <div>${film.Year}</div>
                    <button>Details</button>
                </div>
            </div>`;
    }
});
