using System.Collections.ObjectModel;
using System.Text.Json;

namespace Gor_app_midterm;

public class GroceryItem
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Image { get; set; } = "";
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public string Description { get; set; } = "Fresh everyday grocery essential.";
    public bool IsOnSale => OriginalPrice > Price;
    public string OriginalPriceText => IsOnSale ? $"Was {OriginalPrice:C2}" : "";
    public int Quantity { get; set; } = 1;
    public string PriceText => Price.ToString("C2");
    public string LineTotal => (Price * Quantity).ToString("C2");
}

public partial class MainPage : ContentPage
{
    private readonly List<GroceryItem> products = new()
    {
        new() { Name = "Apples", Category = "Fruits", Price = 2.49m, OriginalPrice = 2.99m, Description = "Crisp, fresh apples. Great for snacks.", Image = "apples.png" },
        new() { Name = "Bananas", Category = "Fruits", Price = 1.49m, Description = "Naturally sweet bananas for breakfast or smoothies.", Image = "bananas.png" },
        new() { Name = "Milk", Category = "Dairy", Price = 3.49m, Description = "Everyday dairy milk for your kitchen.", Image = "milk.png" },
        new() { Name = "Eggs", Category = "Dairy", Price = 3.79m, OriginalPrice = 4.29m, Description = "Eggs for breakfast, baking, and cooking.", Image = "eggs.png" },
        new() { Name = "Bread", Category = "Bakery", Price = 2.50m, Description = "Soft bread for toast and sandwiches.", Image = "bread.png" },
        new() { Name = "Chicken", Category = "Meat", Price = 7.99m, OriginalPrice = 8.99m, Description = "Chicken for your favorite family meals.", Image = "chicken.png" },
        new() { Name = "Rice", Category = "Pantry", Price = 5.49m, Description = "Versatile rice for everyday cooking.", Image = "rice.png" },
        new() { Name = "Orange Juice", Category = "Drinks", Price = 3.99m, Description = "Refreshing orange juice for any morning.", Image = "orange_juice.png" }
    };

    private readonly ObservableCollection<GroceryItem> cart = new();
    private readonly ObservableCollection<string> groceryList = new();
    private string search = "";
    private string category = "All";

    public MainPage()
    {
        InitializeComponent();
        ProductList.ItemsSource = products;
        CartList.ItemsSource = cart;
        ShoppingList.ItemsSource = groceryList;
        LoadSavedList();
        UpdateTotal();
    }

    private void ShowShop(object sender, EventArgs e) => ShowSection(0);
    private void ShowCart(object sender, EventArgs e) => ShowSection(1);
    private void ShowList(object sender, EventArgs e) => ShowSection(2);

    private void ShowSection(int section)
    {
        ShopSection.IsVisible = section == 0;
        CartSection.IsVisible = section == 1;
        ListSection.IsVisible = section == 2;
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        search = e.NewTextValue?.Trim() ?? "";
        ApplyFilters();
    }

    private void FilterCategory(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string selected)
        {
            category = selected;
            ApplyFilters();
        }
    }

    private void ApplyFilters()
    {
        ProductList.ItemsSource = products.Where(p =>
            (category == "All" || p.Category == category) &&
            (p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             p.Category.Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private async void ShowProductDetails(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is GroceryItem item)
        {
            var sale = item.IsOnSale ? $"\nSale! Was {item.OriginalPrice:C2}" : "";
            await DisplayAlertAsync(item.Name,
                $"{item.Category}\n{item.Description}\nPrice: {item.Price:C2}{sale}", "Close");
        }
    }

    private void SaveList()
    {
        Preferences.Default.Set("freshcart_shopping_list", JsonSerializer.Serialize(groceryList.ToList()));
    }

    private void LoadSavedList()
    {
        try
        {
            var json = Preferences.Default.Get("freshcart_shopping_list", "[]");
            foreach (var name in JsonSerializer.Deserialize<List<string>>(json) ?? new())
                groceryList.Add(name);
        }
        catch (JsonException)
        {
            groceryList.Clear();
        }
    }

    private async void Checkout(object sender, EventArgs e)
    {
        if (cart.Count == 0)
        {
            await DisplayAlertAsync("Cart is empty", "Add groceries before checking out.", "OK");
            return;
        }

        var summary = string.Join("\n", cart.Select(p => $"{p.Name} x{p.Quantity}: {p.Price * p.Quantity:C2}"));
        var total = cart.Sum(p => p.Price * p.Quantity);
        bool confirm = await DisplayAlertAsync("Review demo order",
            $"{summary}\n\nTotal: {total:C2}\n\nThis is a demo. No payment will be taken.",
            "Place demo order", "Cancel");
        if (!confirm) return;
        cart.Clear();
        RefreshCart();
        await DisplayAlertAsync("Order complete", "Your demo order has been placed!", "OK");
    }

    private void AddToCart(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not GroceryItem product) return;
        var existing = cart.FirstOrDefault(p => p.Name == product.Name);
        if (existing == null)
            cart.Add(new GroceryItem { Name = product.Name, Category = product.Category, Image = product.Image, Price = product.Price });
        else
            existing.Quantity++;
        RefreshCart();
    }

    private void IncreaseQuantity(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is GroceryItem item)
        {
            item.Quantity++;
            RefreshCart();
        }
    }

    private void DecreaseQuantity(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is GroceryItem item)
        {
            if (item.Quantity > 1) item.Quantity--;
            else cart.Remove(item);
            RefreshCart();
        }
    }

    private void RemoveCartItem(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is GroceryItem item)
        {
            cart.Remove(item);
            RefreshCart();
        }
    }

    private void ClearCart(object sender, EventArgs e)
    {
        cart.Clear();
        RefreshCart();
    }

    private void RefreshCart()
    {
        CartList.ItemsSource = null;
        CartList.ItemsSource = cart;
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        decimal total = cart.Sum(p => p.Price * p.Quantity);
        TotalLabel.Text = $"Total: {total:C2}";
    }

    private void AddListItem(object sender, EventArgs e)
    {
        var name = ListEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        groceryList.Add(name);
        ListEntry.Text = "";
        SaveList();
    }

    private void RemoveListItem(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string name)
        {
            groceryList.Remove(name);
            SaveList();
        }
    }
}
