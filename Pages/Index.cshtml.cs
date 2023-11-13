using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LuceedConnect;

namespace LuceedTest_Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private int _productCountDefault = 5;
        private string _Username = null;
        private string _Password = null;
        private Luceed Luceed;

        public readonly int[] ProductCountOptions = { 1, 5, 10, 25, 50 };
        public int ProductCount { get
            {
                if(HttpContext.Session.GetInt32("ProductCount") != null)
                {
                    return (int)HttpContext.Session.GetInt32("ProductCount");
                }
                else
                {
                    return _productCountDefault;
                }
            } 
        }

        public bool IsAuthenticated {  get
            {
                return (_Username != null && _Password != null);
            } 
        }

        private APIPaging Paging
        {
            get
            {
                return new APIPaging() { StartIndex = (CurrentPage - 1) * ProductCount, Count = ProductCount + 1 };
            }
        }

        [BindProperty]
        public string ArticleQueryString { get; set; }
        [BindProperty]
        public int QueryProductCount { get; set; }

        public List<Product> Products = null;
        public bool HasMoreProducts = false;

        private int _page;
        public int CurrentPage
        {
            get
            {
                if (Request.Query["Page"].Count > 0)
                {

                    if (int.TryParse(Request.Query["Page"].ToString(), out _page))
                        return _page;
                    else
                        return 1;
                }
                return 1;
            }
        }
        public string QueryString
        {
            get
            {
                if (Request.Query["Query"].Count > 0)
                {
                    return Request.Query["Query"][0];
                }
                return null;
            }
        }

        public override void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
            //Dohvaćanje usera iz sesije prije svakog request-a
            _Username = HttpContext.Session.GetString("Username");
            _Password = HttpContext.Session.GetString("Password");

        }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> OnGet()
        {
            if (!IsAuthenticated)
            {
                return Redirect("/Login");
            }

            if (QueryString != null)
            {
                ArticleQueryString = QueryString;
                try
                {
                    Luceed = new Luceed(_Username, _Password);
                    Products = await Luceed.GetProductsByNamePartial(QueryString, Paging);
                    HasMoreProducts = (bool)Luceed.HasMoreProducts;
                }
                catch (Exception err)
                {
                    ModelState.AddModelError("Error", err.Message);
                }
            }
            return Page();
        }

        public IActionResult OnPostFindArticlesByPartOfName()
        {
            if (ArticleQueryString == null) ArticleQueryString = "";

            HttpContext.Session.SetInt32("ProductCount", QueryProductCount);

            return Redirect("/Index?Query="+ArticleQueryString);

        }
    }
}
