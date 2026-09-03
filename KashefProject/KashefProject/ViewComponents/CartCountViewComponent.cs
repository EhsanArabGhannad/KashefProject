using KashefProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace KashefProject.ViewComponents;

public sealed class CartCountViewComponent(ShoppingService shopping) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() => Content((await shopping.GetSummaryAsync()).Count.ToString());
}
