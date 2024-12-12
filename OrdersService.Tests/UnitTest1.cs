using Xunit;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Synapse.OrdersExample;
using Newtonsoft.Json.Linq;


namespace OrdersService.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task TestFetchMedicalEquipmentOrders_Sucess()
        {
            string orders = @"
            [
                { 
                    'OrderId': 1, 
                    'Items': [{ 
                          'ItemId': 1,
                          'Description': 'Wheelchair', 
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        }]
                },
                { 
                    'OrderId': 2, 
                    'Items': [{ 
                          'ItemId': 1,
                          'Description': 'Wheelchair', 
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        },
                        { 
                          'ItemId': 2, 
                          'Description': 'Wheelchair ramp', 
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        },
                        { 
                          'ItemId': 3, 
                          'Description': 'Crutches - Adult',
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        }]
                },
                { 
                    'OrderId': 3, 
                    'Items': [{ 
                          'ItemId': 1, 
                          'Description': 'Wheelchair', 
                          'Status': 'In Transit', 
                          'deliveryNotification': 0
                        }, 
                        { 
                          'ItemId': 2, 
                          'Description': 'Wheelchair ramp', 
                          'Status': 'In Transit', 
                          'deliveryNotification': 0
                        }]
                },
                { 
                    'OrderId': 4, 
                    'Items': [{ 
                          'ItemId': 3, 
                          'Description': 'Crutches - Adult', 
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        }, 
                        { 
                          'ItemId': 2, 
                          'Description': 'Wheelchair ramp', 
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        }]
                }]";

            var httpMessage = new Mock<HttpMessageHandler>();
            httpMessage
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(orders)
                });

            var httpClient = new HttpClient(httpMessage.Object);
            var service = new OrdersProgram(httpClient);

            var result = await service.FetchMedicalEquipmentOrders();

            Assert.NotNull(result);
            Assert.Equal(4, result.Length);
        }

        [Fact]
        public async Task TestFetchMedicalEquipmentOrders_Unsuccessful()
        {
            var httpMessage = new Mock<HttpMessageHandler>();
            httpMessage
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            var httpClient = new HttpClient(httpMessage.Object);
            var service = new OrdersProgram(httpClient);

            var result = await service.FetchMedicalEquipmentOrders();

            Assert.Empty(result);
        }

        [Fact]
        public async Task TestFetchMedicalEquipmentOrders_NetworkError()
        {
            var httpMessage = new Mock<HttpMessageHandler>();
            httpMessage
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network Error"));

            var httpClient = new HttpClient(httpMessage.Object);
            var service = new OrdersProgram(httpClient);

            var result = await service.FetchMedicalEquipmentOrders();
            Assert.Empty(result);
            // assert exception was thrown
        }
        
        [Fact]
        public async Task TestFetchMedicalEquipmentOrders_Exception()
        {
            var httpMessage = new Mock<HttpMessageHandler>();
            httpMessage
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Unexpected Error"));

            var httpClient = new HttpClient(httpMessage.Object);
            var service = new OrdersProgram(httpClient);

            var result = await service.FetchMedicalEquipmentOrders();
            Assert.Empty(result);
            // assert exception was thrown
        }

        [Fact]
        public void TestProcessOrder_Success()
        {
            var orderJson = @"
            {
                'OrderId' : 1234,
                'Items' : [
                    { 'ItemId' : 1, 'Description': 'Wheelchair', 'Status': 'Delivered', 'deliveryNotification': 0},
                    { 'ItemId' : 2, 'Description': 'Wheelchair ramp', 'Status': 'Delivered', 'deliveryNotification': 0}
                ]
            }";

            var httpMessage = new Mock<HttpMessageHandler>();
            httpMessage
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.Content.ReadAsStringAsync().Result.Contains("Alert for delivered item: Order 1234")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                });

            var httpClient = new HttpClient(httpMessage.Object);
            var service = new OrdersProgram(httpClient);

            var order = JObject.Parse(orderJson);
            var result = service.ProcessOrder(order);
            var items = result["Items"].ToObject<JArray>();
            
            var item1 = items[0];
            Assert.Equal("Wheelchair", item1["Description"].ToString());
            Assert.Equal("Delivered", item1["Status"].ToString());
            Assert.Equal("1", item1["deliveryNotification"].ToString());

            var item2 = items[1];
            Assert.Equal("Wheelchair ramp", item2["Description"].ToString());
            Assert.Equal("Delivered", item2["Status"].ToString());
            Assert.Equal("1", item2["deliveryNotification"].ToString());
        }

        [Fact]
        public void TestProcessOrder_Unsuccessful()
        {
            var orderJson = @"
            {
                'OrderId' : 1234,
                'Items' : [
                    { 'ItemId' : 1, 'Description': 'Wheelchair', 'Status': 'In Transit', 'deliveryNotification': 0},
                    { 'ItemId' : 2, 'Description': 'Wheelchair ramp', 'Status': 'In Transit', 'deliveryNotification': 0}
                ]
            }"; ;

            var service = new OrdersProgram();

            var order = JObject.Parse(orderJson);
            var result = service.ProcessOrder(order);
            var items = result["Items"].ToObject<JArray>();

            // request to alert-api was unsuccessful, therefore deliveryNotification never got updated
            var item1 = items[0];
            Assert.NotEqual("Delivered", item1["Status"].ToString());
            Assert.Equal("0", item1["deliveryNotification"].ToString());

            var item2 = items[1];
            Assert.NotEqual("Delivered", item1["Status"].ToString());
            Assert.Equal("0", item1["deliveryNotification"].ToString());
        }

        [Fact]
        public void TestProcessOrder_BadRequest()
        {
            var orderJson = @"
            {
                'OrderId' : 1234,
                'Items' : [
                    { 'ItemId' : 1, 'Description': 'Wheelchair', 'Status': 'Delivered', 'deliveryNotification': 0},
                    { 'ItemId' : 2, 'Description': 'Wheelchair ramp', 'Status': 'Delivered', 'deliveryNotification': 0}
                ]
            }"; ;

            var httpMessage = new Mock<HttpMessageHandler>();
            httpMessage
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            var httpClient = new HttpClient(httpMessage.Object);
            var service = new OrdersProgram(httpClient);

            var order = JObject.Parse(orderJson);
            var result = service.ProcessOrder(order);
            var items = result["Items"].ToObject<JArray>();

            // request to alert-api was unsuccessful, therefore deliveryNotification never got updated
            var item1 = items[0];
            Assert.Equal("Delivered", item1["Status"].ToString());
            Assert.Equal("0", item1["deliveryNotification"].ToString());

            var item2 = items[1];
            Assert.Equal("Delivered", item1["Status"].ToString());
            Assert.Equal("0", item1["deliveryNotification"].ToString());
        }

        [Fact]
        public async void TestSendAlertAndUpdateOrder_Success()
        {
            var orderJson = @"
            {
                'OrderId' : 1234,
                'Items' : [
                    { 'ItemId' : 1, 'Description': 'Wheelchair', 'Status': 'Delivered', 'deliveryNotification': 0},
                    { 'ItemId' : 2, 'Description': 'Wheelchair ramp', 'Status': 'Delivered', 'deliveryNotification': 0}
                ]
            }"; ;

            var httpMessage = new Mock<HttpMessageHandler>();
            httpMessage
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                });

            var httpClient = new HttpClient(httpMessage.Object);
            var service = new OrdersProgram(httpClient);

            var order = JObject.Parse(orderJson);
            await service.SendAlertAndUpdateOrder(order);

            httpMessage.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString() == "https://update-api.com/update"),
                ItExpr.IsAny<CancellationToken>());
        }

        private static String GetListOfOrders()
        {
            string orders = @"
            [
                { 
                    'OrderId': 1, 
                    'Items': [{ 
                          'ItemId': 1,
                          'Description': 'Wheelchair', 
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        }]
                },
                { 
                    'OrderId': 2, 
                    'Items': [{ 
                          'ItemId': 1,
                          'Description': 'Wheelchair', 
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        },
                        { 
                          'ItemId': 2, 
                          'Description': 'Wheelchair ramp', 
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        },
                        { 
                          'ItemId': 3, 
                          'Description': 'Crutches - Adult',
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        }]
                },
                { 
                    'OrderId': 3, 
                    'Items': [{ 
                          'ItemId': 1, 
                          'Description': 'Wheelchair', 
                          'Status': 'In Transit', 
                          'deliveryNotification': 0
                        }, 
                        { 
                          'ItemId': 2, 
                          'Description': 'Wheelchair ramp', 
                          'Status': 'In Transit', 
                          'deliveryNotification': 0
                        }]
                },
                { 
                    'OrderId': 4, 
                    'Items': [{ 
                          'ItemId': 3, 
                          'Description': 'Crutches - Adult', 
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        }, 
                        { 
                          'ItemId': 2, 
                          'Description': 'Wheelchair ramp', 
                          'Status': 'Delivered', 
                          'deliveryNotification': 0
                        }]
                }]";

            return orders;
        }

        private static String GetSingleOrder()
        {
            string order = @"
            {
                'OrderId' : 1234,
                'Items' : [
                    { 'ItemId' : 1, 'Description': 'Wheelchair', 'Status': 'Delivered', 'deliveryNotification': 0},
                    { 'ItemId' : 2, 'Description': 'Wheelchair ramp', 'Status': 'Delivered', 'deliveryNotification': 0}
                ]
            }";

            return order;
        }

        private static String GetSingleItem()
        {
            string item = @"
            {
                'ItemId' : 2, 'Description': 'Wheelchair ramp', 'Status': 'Delivered', 'deliveryNotification': 0
            }";

            return item;
        }
    }



}


