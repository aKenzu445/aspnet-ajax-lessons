using Lesson1.WebServer.Data;
using Lesson1.WebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lesson1.WebServer.Pages;

// === зан8 зад4: страница категорий ===
// всё как в зан4-6: DI в конструкторе, [BindProperty], тег-хелперы,
// OnGet - список, OnPost - добавить с проверкой, TempData + редирект
public class CategoriesModel : PageModel
{
    private readonly TaskDbContext _db;

    public CategoriesModel(TaskDbContext db)
    {
        _db = db;
    }

    // все категории для таблички
    public List<Category> AllCategories { get; set; } = new();

    // новая категория из формы (привязка модели)
    [BindProperty]
    public Category NewCategory { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        // Include(c => c.Tasks) - подтягиваю задачи категории, чтоб показать сколько их
        AllCategories = await _db.Categories.Include(c => c.Tasks).OrderBy(c => c.Id).ToListAsync();
        ViewData["Description"] = "Список категорий задач: добавляйте новые без правки кода и миграций";
    }

    // добавить категорию
    public async Task<IActionResult> OnPostAsync()
    {
        NewCategory.Name = (NewCategory.Name ?? "").Trim();

        // одинаковых названий не надо - а то вернется проблема из зад1
        if (NewCategory.Name != "" && await _db.Categories.AnyAsync(c => c.Name == NewCategory.Name))
        {
            ModelState.AddModelError("NewCategory.Name", "Такая категория уже есть");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        _db.Categories.Add(new Category { Name = NewCategory.Name });
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Категория «{NewCategory.Name}» добавлена";
        return RedirectToPage();
    }

    // === зан8 зад11: удаление категории ===
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        Category? category = await _db.Categories.FindAsync(id);
        if (category == null)
        {
            TempData["Error"] = "Категория не найдена";
            return RedirectToPage();
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Категория «{category.Name}» удалена";
        return RedirectToPage();
    }
}
