using STS.WALAA.Models;
using STS.WALAA.Proxy.Security;
using System;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var proxySettingsSection = configuration.GetSection("ProxySettings");
var proxySettings = proxySettingsSection.Get<ProxySettings>() ?? new();

//using (HttpClient client = new HttpClient())
//{
//    HttpResponseMessage response = await client.GetAsync("https://sharepoint.walaa.com/sites/crm/incident/24-CMP-9592_0BFF04535629EF11867E005056976A38/DA110624255.pdf");
//    response.EnsureSuccessStatusCode();
//    string responseBody = await response.Content.ReadAsStringAsync();
    
//}

builder.Services.Configure<ProxySettings>(proxySettingsSection);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//var token = Encoding.UTF8.GetString(Convert.FromBase64String("UGYyTFl3VFVtUUI0UFBCZEVTYVZBUEFGVUlPak03WTN0VmpwQ2xndG1oTnJUUkJH"));
//var key = token[..32];
//var iv = token[32..];
//var xx = new AES256(key, iv).Encrypt("");
//return;

builder.Services.AddSingleton(new AES256("Pf2LYwTUmQB4PPBdESaVAPAFUIOjM7Y3", "tVjpClgtmhNrTRBG"));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();

app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapControllers();

app.Run();
