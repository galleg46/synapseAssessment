using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

namespace Synapse.OrdersExample
{
    /// <summary>
    /// I Get a list of orders from the API
    /// I check if the order is in a delviered state, If yes then send a delivery alert and add one to deliveryNotification
    /// I then update the order.   
    /// </summary>
    public class OrdersProgram
    {
        private readonly HttpClient _httpClient;
        private static readonly ILogger _logger = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
        }).CreateLogger<OrdersProgram>();

        // shouldn't mock static methods. Making methods non-static and injecting HttpClient as a dependency
        public OrdersProgram(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public OrdersProgram() { }


        static int Main(string[] args)
        {
            Console.WriteLine("Start of App");
            var service = new OrdersProgram();


            var medicalEquipmentOrders = service.FetchMedicalEquipmentOrders().GetAwaiter().GetResult();
            foreach (var order in medicalEquipmentOrders)
            {
                var updatedOrder = service.ProcessOrder(order);

                if (updatedOrder["processStatus"].ToString().Equals("not processed") || updatedOrder["processStatus"].ToString().Equals("partial"))
                {
                    continue;
                }

                service.SendAlertAndUpdateOrder(updatedOrder).GetAwaiter().GetResult();
            }

            Console.WriteLine("Results sent to relevant APIs.");
            return 0;
        }

        public async Task<JObject[]> FetchMedicalEquipmentOrders()
        {
            string ordersApiUrl = "https://orders-api.com/orders";

            try
            {
                var response = await _httpClient.GetAsync(ordersApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch orders from API");
                    return new JObject[0];
                }

                var ordersData = await response.Content.ReadAsStringAsync();
                return JArray.Parse(ordersData).ToObject<JObject[]>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error occured while fetching orders from API.");
                return new JObject[0];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occured while fetching orders from API.");
                return new JObject[0];
            }

        }

        public JObject ProcessOrder(JObject order)
        {
            var items = order["Items"].ToObject<JArray>();
            int counter = 0;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (IsItemDelivered(item))
                {
                    SendAlertMessage(item, order["OrderId"].ToString());

                    items[i] = item;
                    counter++;
                }
            }

            if (counter < items.Count)
            {
                order["processStatus"] = "partial";
            }
            else if (counter == 0)
            {
                order["processStatus"] = "not processed";
            }
            else
            {
                order["processStatus"] = "processed";
            }

            order["Items"] = items;

            return order;
        }

        public bool IsItemDelivered(JToken item)
        {
            return item["Status"].ToString().Equals("Delivered", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Delivery alert
        /// </summary>
        /// <param name="orderId">The order id for the alert</param>
        public void SendAlertMessage(JToken item, string orderId)
        {
            string alertApiUrl = "https://alert-api.com/alerts";

            var alertData = new
            {
                Message = $"Alert for delivered item: Order {orderId}, Item: {item["Description"]}, " +
                          $"Delivery Notifications: {item["deliveryNotification"]}"
            };
            var content = new StringContent(JObject.FromObject(alertData).ToString(), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = _httpClient.PostAsync(alertApiUrl, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    IncrementDeliveryNotification(item);
                }
                else
                {
                    _logger.LogWarning($"Failed to send alert for delivered item: {item["Description"]}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error occured while sending alert message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occured while sending alert message");
            }
        }

        public void IncrementDeliveryNotification(JToken item)
        {
            item["deliveryNotification"] = item["deliveryNotification"].Value<int>() + 1;
        }

        public async Task SendAlertAndUpdateOrder(JObject order)
        {
            string updateApiUrl = "https://update-api.com/update";

            var content = new StringContent(order.ToString(), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(updateApiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"""Failed to send updated order for processing: OrderId {order["OrderId"]}""");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"""Network error occured while sending updated order: OrderId {order["OrderId"]}""");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"""Unexpected error occured while sending updated order: OrderId {order["OrderId"]}""");
            }

        }
    }
}