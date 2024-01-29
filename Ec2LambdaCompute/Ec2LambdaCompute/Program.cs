using Amazon.Lambda;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IAmazonS3, AmazonS3Client>();
builder.Services.AddTransient<IAmazonLambda, AmazonLambdaClient>();
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policyBuilder => policyBuilder.WithOrigins("*").AllowAnyHeader().AllowAnyMethod());
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
