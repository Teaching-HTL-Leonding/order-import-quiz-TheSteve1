using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

var factory = new CookbookContextFactory();
using var context = factory.CreateDbContext(args);

if (args[0].Equals("import"))
{
	Import();
}else if (args[0].Equals("clean"))
{
	Clean();
}
else if (args[0].Equals("check"))
{
	Check();
}
else if (args[0].Equals("full"))
{
	Clean();
	Import();
	Check();
}

async void Import()
{
	string[] customers = await File.ReadAllLinesAsync(args[1]);
	string[] orders = await File.ReadAllLinesAsync(args[2]);
	customers.Skip(1).ToList().ForEach(l =>{
		string[] col = l.Split("\t");
		context.Customers.Add(new Customer(col[0], decimal.Parse(col[1])));
	});
	//await context.SaveChangesAsync(); (überträgt die Daten nicht in die Datenbank)
	//falls Sie Lösungsvorschläge hätten, bitte per Issue mitteilen. 
	context.SaveChanges();
	orders.Skip(1).ToList().ForEach(l => {
		string[] col = l.Split("\t");
		Customer customer = context.Customers.First(i => i.Name == col[0]);
		context.Orders.Add(new Order(customer.Id,DateTime.Parse(col[1]),decimal.Parse(col[2])));
	});
	//await context.SaveChangesAsync(); (überträgt die Daten nicht in die Datenbank)
	//falls Sie Lösungsvorschläge hätten, bitte per Issue mitteilen.  
	context.SaveChanges();
}
//Ich habe keine ahnung warum async methoden nicht funktionieren
 void Clean()
{
	DbSet<Customer> customers = context.Customers;
	DbSet<Order> orders = context.Orders;
	foreach(Customer customer in customers)
	context.Customers.Remove(customer);
	foreach(Order order in orders)
	context.Orders.Remove(order);
	context.SaveChanges();
}
void Check()
{
	DbSet<Customer> customers = context.Customers;
	Console.WriteLine("Customers	Limit");
	context.Orders
		.GroupBy(i => i.CustomerId)
		.Select(order => new
		{
			CId = order.Key,
			Limit = customers.First(i => i.Id == order.Key).CreditLimit,
			Sum = order.Sum(i => i.OrderValue),
			Diff = customers.First(i => i.Id == order.Key).CreditLimit - order.Sum(i => i.OrderValue)
		})
		.Where(i => i.Sum > i.Limit)
		.ToList()
		.ForEach(customer => Console.WriteLine($"{customers.First(i => i.Id == customer.CId).Name}	{customer.Diff}"));
}
class Customer
{
	public int Id { get; set; }
	[MaxLength(100)]
	public string Name { get; set; } = string.Empty;
	[Column(TypeName = "decimal(8,2)")]
	public decimal CreditLimit { get; set; }

	public Customer(string name, decimal creditLimit)
	{
		Name = name;
		CreditLimit = creditLimit;
	}
}
class Order
{
	public int Id { get; set; }
	public int CustomerId { get; set; }
	[MaxLength(50)]
	public DateTime OrderDate { get; set; }
	[Column(TypeName = "decimal(8,2)")]
	public decimal OrderValue { get; set; }

	public Order(int customerId, DateTime orderDate, decimal orderValue)
	{
		CustomerId = customerId;
		OrderDate = orderDate;
		OrderValue = orderValue;
	}
}
class OrderInportContext : DbContext
{
	public DbSet<Customer> Customers { get; set; }
	public DbSet<Order> Orders { get; set; }

#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
	public OrderInportContext(DbContextOptions<OrderInportContext> options)
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
		: base(options)
	{ }
}
class CookbookContextFactory : IDesignTimeDbContextFactory<OrderInportContext>
{
	public OrderInportContext CreateDbContext(string[] args)
	{
		var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

		var optionsBuilder = new DbContextOptionsBuilder<OrderInportContext>();
		optionsBuilder
			// Uncomment the following line if you want to print generated
			// SQL statements on the console.
			//.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
			.UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

		return new OrderInportContext(optionsBuilder.Options);
	}
}