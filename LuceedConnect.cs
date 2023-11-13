using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LuceedConnect
{
    public class APIPaging
    {
        public int StartIndex { get; set; } = 0;
        public int Count { get; set; } = 21;

        public override string ToString()
        {
            return $"[{StartIndex},{Count}]";
        }
    }

    public class DateRange
    {
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public DateRange(DateTime from, DateTime? to)
        {
            From = from;
            To = to;
        }
    }

    public class Base64AuthManager
    {
        public string Username { get; set; } = null;
        public string Password { get; set; } = null;

        public Base64AuthManager (string _username, string _password)
        {
            if(string.IsNullOrEmpty(_username))
            {
                throw new Exception("Username can not be empty");
            }
            if (string.IsNullOrEmpty(_password))
            {
                throw new Exception("Password can not be empty");
            }

            Username = _username;
            Password = _password;
        }
        public bool IsAuthenticated { get
            {
                return Username != null && Password != null;
            }
        }
        public string Credentials
        {
            get
            {
                if (Username != null && Password != null)
                {
                    return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Password}"));
                }
                else
                {
                    return null;
                }
            }
        }
        public System.Net.Http.Headers.AuthenticationHeaderValue AuthorizationHeader
        {
            get
            {
                if(Credentials != null)
                {
                    return new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Credentials);
                }
                else
                {
                    throw new Exception("Not authenticated to API");
                }
            }
        }

    }
    public class Luceed
    {
        private Base64AuthManager Auth { get; set; }
        public bool? HasMoreProducts { get; set; } = null;

        public Luceed(string _username, string _password)
        {
            Auth = new Base64AuthManager(_username, _password);
        }

        public async Task<List<Product>> GetProductsByNamePartial(string prompt, APIPaging page)
        {
            var apiUrl = $"http://apidemo.luceed.hr/datasnap/rest/artikli/naziv/{prompt}/{page}";

            Result result = await GetResponseFromAPI(HttpMethod.Get, apiUrl);

            if(result.Products.Count == page.Count)
            {
                HasMoreProducts = true;
                result.Products.RemoveAt(page.Count - 1);
            }
            else
            {
                HasMoreProducts = false;
            }

            return result.Products;
        }

        public async Task<List<SalePoint>> GetSalePointsFromWarehouses()
        {
            Result result = await GetResponseFromAPI(HttpMethod.Get, "http://apidemo.luceed.hr/datasnap/rest/skladista/lista");

            if (result.SalePoints != null)
            {
                
                return result.SalePoints.Distinct(new SalePointEqualityComparer()).ToList();
            }
            else
            {
                return new List<SalePoint>();
            }
        }

        public async Task<List<PaymentCalculation>> GetPaymentCalculationsByPaymentType(string salePoint, DateRange Range)
        {
            if(Range.From > DateTime.Now)
            {
                throw new Exception("From date can not be in the future");
            }
            if (Range.To != null)
            {
                if (Range.To <= Range.From)
                {
                    throw new Exception("To date has to be after from date");
                }
            }

            var endpointBuilder = $"http://apidemo.luceed.hr/datasnap/rest/mpobracun/placanja/{salePoint}/";
            endpointBuilder += Range.From.ToString("d.M.yyyy");
            if (Range.To != null)
            {
                endpointBuilder += "/" + ((DateTime)Range.To).ToString("d.M.yyyy");
            }

            Result result = await GetResponseFromAPI(HttpMethod.Get, endpointBuilder);

            if (result.PaymentTypeCalculations != null)
            {
                return result.PaymentTypeCalculations;
            }

            return new List<PaymentCalculation>();
        }

        public async Task<List<PaymentCalculationArticle>> GetPaymentCalculationsByArticle(string salePoint, DateRange Range)
        {
            if (Range.From > DateTime.Now)
            {
                throw new Exception("From date can not be in the future");
            }
            if (Range.To != null)
            {
                if (Range.To <= Range.From)
                {
                    throw new Exception("To date has to be after from date");
                }
            }

            var endpointBuilder = $"http://apidemo.luceed.hr/datasnap/rest/mpobracun/artikli/{salePoint}/";
            endpointBuilder += Range.From.ToString("d.M.yyyy");
            if (Range.To != null)
            {
                endpointBuilder += "/" + ((DateTime)Range.To).ToString("d.M.yyyy");
            }

            Result result = await GetResponseFromAPI(HttpMethod.Get, endpointBuilder);

            if (result.PaymentTypeArticles != null)
            {
                return result.PaymentTypeArticles;
            }

            return new List<PaymentCalculationArticle>();
        }

        private async Task<Result> GetResponseFromAPI(HttpMethod method, string url)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    Method = method,
                    RequestUri = new Uri(url),
                };

                request.Headers.Authorization = Auth.AuthorizationHeader;


                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    Results results = JsonConvert.DeserializeObject<Results>(json);

                    if (results.ResultSets.Count == 0)
                    {
                        throw new Exception("Result set not found");
                    }

                    return results.ResultSets[0];
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(response);
                    throw new Exception("An error has occured while retrieving data from API");
                }
            }
        }
    }

    public class Result
    {
        [JsonProperty("artikli")]
        public List<Product> Products { get; set; }
        [JsonProperty("skladista")]
        public List<SalePoint> SalePoints { get; set; }

        [JsonProperty("obracun_placanja")]
        public List<PaymentCalculation> PaymentTypeCalculations { get; set; }
        [JsonProperty("obracun_artikli")]
        public List<PaymentCalculationArticle> PaymentTypeArticles { get; set; }
    }


    public class Results
    {
        [JsonProperty("result")]
        public List<Result> ResultSets { get; set; }
    }

    public class Product
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("naziv")]
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class PaymentType   
    {
        [JsonProperty("vrsta_placanja_uid")]
        public string Id { get; set; }

        [JsonProperty("naziv")]
        public string Name { get; set; }

        [JsonProperty("fiskalna_oznaka")]
        public string FiscalId { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class PaymentCalculation
    {
        [JsonProperty("vrste_placanja_uid")]
        public string Id { get; set; }

        [JsonProperty("naziv")]
        public string Name { get; set; }

        [JsonProperty("Iznos")]
        public double Amount { get; set; }

        public override string ToString()
        {
            return Amount.ToString();
        }
    }
    public class PaymentCalculationArticle
    {
        [JsonProperty("artikl_uid")]
        public string Id { get; set; }

        [JsonProperty("naziv_artikla")]
        public string Name { get; set; }

        [JsonProperty("kolicina")]
        public double Amount { get; set; }

        [JsonProperty("iznos")]
        public double Total { get; set; }

        [JsonProperty("usluga")]
        private string P_service { get; set; }
        public bool Service { get
            {
                return P_service == "D";
            } 
        }

        public override string ToString()
        {
            return Total.ToString();
        }
    }


    public class SalePointEqualityComparer : IEqualityComparer<SalePoint>
    {
        public bool Equals(SalePoint x, SalePoint y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            return x.SalePointId == y.SalePointId;
        }

        public int GetHashCode(SalePoint obj)
        {
            return obj?.SalePointId?.GetHashCode() ?? 0;
        }
    }

    public class SalePoint
    {
        [JsonProperty("pj_naziv")]
        public string Name { get; set; }

        [JsonProperty("pj_uid")]
        public string SalePointId { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }


}
