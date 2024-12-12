## OrdersService
OrdersService contains the refactored C# file that was assigned

## OrdersService.Tests
OrdersService.Tests consists of one test file. The test file contains 8 unit tests for OrdersService/OrdersProgram.cs

## OrdersServiceMocked
OrdersServiceMocked consists of one C# file that is a "mocked" version of OrdersProgram.cs. This file assumes all http calls return a 200 OK. 
The main method calls a private GET function to simulate the GET API call. The GET function provides the data for the program.

When executed, the program will only output orders that have been processed to the console.
