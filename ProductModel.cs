using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using whstore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// Register repository implementation.
builder.Services.AddSingleton<IProductRepository, ProductRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Map MVC controllers (HomeController) and Razor Pages.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace whstore
{
    public class ProductRepository : IProductRepository
    {
        private readonly ConcurrentDictionary<int, ProductModel> _store = new();
        private int _nextId;

        public Task<IEnumerable<ProductModel>> GetAllAsync()
        {
            var snapshot = _store.Values.ToArray();
            return Task.FromResult<IEnumerable<ProductModel>>(snapshot);
        }

        public Task<ProductModel?> GetByIdAsync(int id)
        {
            _store.TryGetValue(id, out var model);
            return Task.FromResult(model);
        }

        public Task AddAsync(ProductModel product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            var id = Interlocked.Increment(ref _nextId);
            product.Id = id;
            _store[id] = product;
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(ProductModel product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            if (product.Id <= 0) return Task.FromResult(false);

            _store.AddOrUpdate(product.Id, product, (_, __) => product);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int id)
        {
            return Task.FromResult(_store.TryRemove(id, out _));
        }
    }
}

using System;

namespace whstore
{
    public class ProductModel
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

using Microsoft.AspNetCore.Mvc;

namespace whstore.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: /Home/AdminPrivacy  -> returns Views/Home/Admin/Privacy.cshtml
        public IActionResult AdminPrivacy()
        {
            return View("Admin/Privacy");
        }

        // Simple error view
        public IActionResult Error()
        {
            return View();
        }
    }
}

@{
    Layout = "_Layout";
    ViewData["Title"] = "Privacy - Admin";
}

<h1>Admin Privacy</h1>

<p>This is the admin privacy view. Remove any pasted terminal commands or stray XML from this file � it should contain only valid Razor/HTML markup.</p>

<h2>Clone the Repository</h2>
<pre><code>git clone https://github.com/whidevai-cell/your-repo-name.git
cd your-repo-name
dotnet restore
dotnet build</code></pre>

<h2>Restore, Build, and Run</h2>
<pre><code>dotnet restore
dotnet build
dotnet run   # or dotnet watch run for hot reload</code></pre>

Select-String -Path .\**\*.cs -Pattern '<PropertyGroup>|dotnet |^cd "' -List | Select-Object Path -Unique

git apply fix.patch
git add .
git commit -m "Fix: remove pasted terminal/XML and correct namespaces"
git push origin YOUR_BRANCH