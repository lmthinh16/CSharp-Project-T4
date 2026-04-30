using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using web_vk.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class RestaurantsModel : PageModel
{
    private readonly AppDbContext _context;

    public List<Restaurant> list { get; set; } = new();

    public RestaurantsModel(AppDbContext context)
    {
        _context = context;
    }

    // LOAD DATA
    public void OnGet()
    {
        list = _context.Restaurants.ToList();
    }

    // THÊM DATA
    public IActionResult OnPost(string name, string address)
    {
        var r = new Restaurant
        {
            Name = name,
            Address = address,
            AudioPath = "https://translate.google.com/translate_tts?ie=UTF-8&q="
                        + Uri.EscapeDataString(name + " tại " + address)
                        + "&tl=vi&client=tw-ob"
        };

        _context.Restaurants.Add(r);
        _context.SaveChanges();

        return RedirectToPage();
    }
}