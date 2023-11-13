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
using System.Globalization;

namespace LuceedTest_Web.Pages
{
    public enum CalculationType
    {
        MissingType,
        ByPaymentType,
        ByArticle
    }
    public class CalculationModel : PageModel
    {
        private readonly ILogger<CalculationModel> _logger;

        private string _Username = null;
        private string _Password = null;
        private Luceed Luceed;

        public bool IsAuthenticated {  get
            {
                return (_Username != null && _Password != null);
            } 
        }

        public List<SalePoint> SalePoints { get; private set; } = null;

        [BindProperty]
        public CalculationType Form_CalculationType { get; set; }
        [BindProperty]
        public string Form_SalePointId { get; set; }
        [BindProperty]
        public DateTime Form_DateFrom { get; set; } = new DateTime(DateTime.Now.Year - 10, 1, 1);
        [BindProperty]
        public DateTime? Form_DateTo { get; set; } = null;



        public CalculationType QueryCalculationType
        {
            get
            {

                if (Request.Query["CalculationType"].Count > 0)
                {
                    int type;
                    if (int.TryParse(Request.Query["CalculationType"][0], out type))
                    {
                        return (CalculationType)Enum.ToObject(typeof(CalculationType), type); ;
                    }
                }
                return CalculationType.ByPaymentType;
            }
        }
        public string QuerySalePoint
        {
            get
            {
                if (Request.Query["SalePoint"].Count > 0)
                {
                    return Request.Query["SalePoint"][0];
                }
                return null;
            }
        }
        public DateTime QueryDateFrom
        {
            get
            {
                if (Request.Query["DateFrom"].Count > 0)
                {
                    DateTime _date;
                    if (DateTime.TryParseExact(Request.Query["DateFrom"][0], "d.M.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _date))
                    {
                        return _date;
                    }
                    else
                    {
                        //default 1.1. prije 10 godina
                        return new DateTime(DateTime.Now.Year - 10, 1, 1);
                    }
                }
                return new DateTime(DateTime.Now.Year - 10, 1, 1);
            }
        }
        public DateTime? QueryDateTo
        {
            get
            {
                if (Request.Query["DateTo"].Count > 0)
                {
                    DateTime _date;
                    if (DateTime.TryParseExact(Request.Query["DateTo"][0], "d.M.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _date))
                    {
                        return _date;
                    }
                    else
                    {
                        return null;
                    }
                }
                return null;
            }
        }

        public List<PaymentCalculation> PaymentCalculationsByPaymentType { get; private set; } = null;
        public List<PaymentCalculationArticle> PaymentCalculationsByArticles { get; private set; } = null;
        

        public override void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
            _Username = HttpContext.Session.GetString("Username");
            _Password = HttpContext.Session.GetString("Password");
        }

        public CalculationModel(ILogger<CalculationModel> logger)
        {
            _logger = logger;
        }

        public string Test;

        public async Task<IActionResult> OnGet()
        {
            Form_CalculationType = QueryCalculationType;
            Form_DateFrom = QueryDateFrom;
            Form_DateTo = QueryDateTo;
            if (IsAuthenticated)
            {
                Luceed = new Luceed(_Username, _Password);
                try
                {
                    SalePoints = await Luceed.GetSalePointsFromWarehouses();
                    if(QuerySalePoint != null && QueryDateFrom != null)
                    {
                        Form_SalePointId = QuerySalePoint;
                        switch (QueryCalculationType)
                        {
                            case CalculationType.ByPaymentType:
                                PaymentCalculationsByPaymentType = await Luceed.GetPaymentCalculationsByPaymentType(QuerySalePoint, new DateRange(QueryDateFrom, QueryDateTo));
                                break;
                            case CalculationType.ByArticle:
                                PaymentCalculationsByArticles = await Luceed.GetPaymentCalculationsByArticle(QuerySalePoint, new DateRange(QueryDateFrom, QueryDateTo));
                                break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    if(exception.Message.StartsWith("From date"))
                    {
                        ModelState.AddModelError("Form_DateFrom", exception.Message);
                    }
                    else if (exception.Message.StartsWith("To date"))
                    {
                        ModelState.AddModelError("Form_DateTo", exception.Message);
                    } 
                    else
                    {
                        ModelState.AddModelError("Error", exception.Message);
                    }
                    System.Diagnostics.Debug.WriteLine(exception.Message);
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostTransactionsCalculation()
        {
            if(Form_DateFrom == null)
            {
                ModelState.AddModelError("Form_DateFrom", "From date can not be empty");
            }
            if (Form_DateTo != null)
            {
                if(Form_DateTo < Form_DateFrom)
                {
                    ModelState.AddModelError("Form_DateTo", "To date can not be before from date");
                }
            }

            if(ModelState.ErrorCount > 0)
            {
                Luceed = new Luceed(_Username, _Password);
                SalePoints = await Luceed.GetSalePointsFromWarehouses();
                    return Page();
            }

            return Redirect($"/Calculations?CalculationType={(int)Form_CalculationType}&SalePoint={Form_SalePointId}&DateFrom={Form_DateFrom.ToString("d.M.yyyy")}{(Form_DateTo != null ? $"&DateTo="+((DateTime)Form_DateTo).ToString("d.M.yyyy") : "" )}");
        }
    }
}
