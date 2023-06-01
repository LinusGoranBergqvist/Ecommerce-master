using Ecommerce_Project.Models;
using Ecommerce_Project.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce_Project.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationContext _context;

        public CheckoutController(ApplicationContext context)
        {
            _context = context;
        }

        public IActionResult Index(int cartId)
        {
            Cart cart = _context.Carts.Include(c => c.CartItems).ThenInclude(ci => ci.Product).FirstOrDefault(c => c.Id == cartId);

            OrderCartItem orderCartView = new OrderCartItem();
            orderCartView.Order = new Order();
            orderCartView.CartItems = cart.CartItems;
            return View(orderCartView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitOrder(OrderCartItem viewModel, int cartId)
        {
			// Räkna ut totalpriset på ordern + frakt
			decimal totalPriceSEK = viewModel.CartItems.Sum(ci => ci.Product.PriceSEK * ci.Quantity) + viewModel.Order.ShippingRate;
			viewModel.Order.TotalPriceSEK = totalPriceSEK;
			viewModel.Order.TotalPriceEUR = totalPriceSEK * 0.1m;
			// Spara beställningen till databasen
			_context.Orders.Add(viewModel.Order);
            _context.SaveChanges();

            // Skapa OrderItems för varje CartItem
            foreach (CartItem cartItem in viewModel.CartItems)
            {
                OrderItem orderItem = new OrderItem();
                orderItem.OrderId = viewModel.Order.Id;
                orderItem.ProductId = cartItem.ProductId;
                orderItem.Quantity = cartItem.Quantity;


                // Spara OrderItem till databasen
                _context.OrderItems.Add(orderItem);
            }

            // Spara ändringarna i databasen
            _context.SaveChanges();

             // Ta bort alla CartItems från användarens kundvagn
             List<CartItem> cartItemsToRemove = _context.CartItems.Where(ci => ci.CartId == viewModel.CartId).ToList();
             foreach (var cartItem in cartItemsToRemove)
             {
                  _context.CartItems.Remove(cartItem);
             }
              _context.SaveChanges();

            // Skicka kunden till startsidan om allt går igenom
            return View("Thanks", viewModel);

        }
    }
}
