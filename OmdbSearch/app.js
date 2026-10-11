// адрес api, ключ OMDB_API_KEY лежит в config.js
const API_URL = "https://www.omdbapi.com/";

// ===== ШАГ 1: при клике на Search отправить запрос и вывести ответ в консоль =====
// Заполни пропуски ___ (подсказки — в чате)

document.getElementById("searchBtn").addEventListener("click", async () => {
    const title = document.getElementById("title").value;
    const type = document.getElementById("type").value;

    const url = `${API_URL}?apikey=${OMDB_API_KEY}&s=${encodeURIComponent(title)}&type=${type}&page=1`;

    const response = await fetch(url);
    const data = await response.json();

    console.log(data);
});
