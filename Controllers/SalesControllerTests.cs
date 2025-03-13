using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Configuration;
using Bogus;
using SalesTest.Controllers;
using SalesTest.Entities;
using SalesTest.Services;

public class SalesControllerTests
{
    private readonly SaleDbContext _dbContext;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly MongoDbService _mongoDbService;
    private readonly SaleEventLogService _saleEventLogService;
    private readonly SaleService _saleService;
    private readonly SalesController _controller;

    private readonly Faker<Sale> _saleFaker;

    public SalesControllerTests()
    {
        var options = new DbContextOptionsBuilder<SaleDbContext>()
            .UseInMemoryDatabase(databaseName: "salesdb")
            .Options;

        _dbContext = new SaleDbContext(options);

        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["MongoSettings:ConnectionString"]).Returns("mongodb://mongouser:mongopassword@localhost:27017");
        _configurationMock.Setup(c => c["MongoSettings:DatabaseName"]).Returns("salesdb");

        _mongoDbService = new MongoDbService(_configurationMock.Object);
        _saleEventLogService = new SaleEventLogService(_mongoDbService);
        _saleService = new SaleService(_dbContext, _saleEventLogService);
        _controller = new SalesController(_saleService);

        _saleFaker = new Faker<Sale>()
    .RuleFor(s => s.Id, f => Guid.NewGuid())
    .RuleFor(s => s.Customer, f => f.Name.FullName())
    .RuleFor(s => s.TotalAmount, f => f.Finance.Amount(10, 1000))
    .RuleFor(s => s.Branch, f => f.Company.CompanyName());
    }

    [Fact]
    public async Task GetAllSales_ReturnsList()
    {
        var sale = _saleFaker.Generate();

        _dbContext.Sales.Add(sale);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetAllSales();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<List<Sale>>(okResult.Value);
    }

    [Fact]
    public async Task GetSaleById_SaleExists_ReturnsSale()
    {
        var sale = _saleFaker.Generate();

        _dbContext.Sales.Add(sale);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetSaleById(sale.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Sale>(okResult.Value);
        Assert.Equal(sale.Id, response.Id);
    }

    [Fact]
    public async Task GetSaleById_SaleNotFound_ReturnsNotFound()
    {
        var result = await _controller.GetSaleById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateSale_ValidSale_ReturnsCreatedSale()
    {
        var newSale = _saleFaker.Generate();

        _controller.ModelState.Clear();

        var result = await _controller.CreateSale(newSale);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<Sale>(createdResult.Value);
        Assert.Equal(newSale.Id, response.Id);
    }

    [Fact]
    public async Task UpdateSale_SaleExists_ReturnsUpdatedSale()
    {
        var sale = _saleFaker.Generate();
        var updatedSale = _saleFaker.Generate();

        _controller.ModelState.Clear();

        _dbContext.Sales.Add(sale);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.UpdateSale(sale.Id, updatedSale);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Sale>(okResult.Value);
        Assert.Equal(sale.Id, response.Id);
        Assert.Equal(updatedSale.Branch, response.Branch);
    }

    [Fact]
    public async Task UpdateSale_SaleNotFound_ReturnsNotFound()
    {
        var result = await _controller.UpdateSale(Guid.NewGuid(), new Sale());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteSale_SaleExists_ReturnsNoContent()
    {
        var sale = _saleFaker.Generate();

        _controller.ModelState.Clear();

        _dbContext.Sales.Add(sale);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.DeleteSale(sale.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteSale_SaleNotFound_ReturnsNotFound()
    {
        var result = await _controller.DeleteSale(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
