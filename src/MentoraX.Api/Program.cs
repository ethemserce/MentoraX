using FluentValidation;
using MentoraX.Api.Middleware;
using MentoraX.Application.Abstractions.Scheduling;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Validation;
using MentoraX.Application.DependencyInjection;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.Auth.Commands;
using MentoraX.Application.Features.MaterialChunks.Commands;
using MentoraX.Application.Features.MaterialChunks.Queries;
using MentoraX.Application.Features.Materials.Commands;
using MentoraX.Application.Features.Materials.Queries;
using MentoraX.Application.Features.Mobile.Commands;
using MentoraX.Application.Features.Mobile.Queries;
using MentoraX.Application.Features.StudyPlans.Commands;
using MentoraX.Application.Features.StudyPlans.Queries;
using MentoraX.Application.Features.StudyProgress.Queries;
using MentoraX.Application.Features.StudySessions.Commands;
using MentoraX.Application.Features.StudySessions.Queries;
using MentoraX.Application.Services;
using MentoraX.Infrastructure.DependencyInjection;
using MentoraX.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scrutor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MentoraX API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste only the JWT token. Swagger adds 'Bearer' automatically.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FlutterWeb",
        policy =>
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
});

builder.Services.AddScoped<IValidationPipeline, ValidationPipeline>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateMaterialCommandValidator>();

builder.Services.AddScoped<IValidationService, ValidationService>();


builder.Services.AddScoped<ICommandHandler<RegisterCommand, AuthResponseDto>, RegisterCommandHandler>();
builder.Services.AddScoped<ICommandHandler<LoginCommand, AuthResponseDto>, LoginCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateMaterialCommand, MaterialDto>, CreateMaterialCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateStudyPlanCommand, StudyPlanDto>, CreateStudyPlanCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CompleteStudySessionCommand, StudySessionDto>, CompleteStudySessionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<StartStudySessionCommand, NextStudySessionDto>, StartStudySessionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<RegisterMobileDeviceCommand, MobileDeviceDto>, RegisterMobileDeviceCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ResumeStudyPlanCommand, int>,ResumeStudyPlanCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateMaterialChunkCommand, MaterialChunkDto>,UpdateMaterialChunkCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CreateMaterialChunkCommand, MaterialChunkDto>,CreateMaterialChunkCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteMaterialChunkCommand, int>,DeleteMaterialChunkCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CompleteStudyPlanCommand, int>,CompleteStudyPlanCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ReorderMaterialChunksCommand, IReadOnlyCollection<MaterialChunkDto>>,ReorderMaterialChunksCommandHandler>();

builder.Services.AddScoped<ICommandHandler<CancelStudyPlanCommand, int>,    CancelStudyPlanCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PauseStudyPlanCommand, int>,PauseStudyPlanCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetStudyPlanByIdQuery, StudyPlanDto?>,GetStudyPlanByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetStudyPlansQuery, IReadOnlyCollection<StudyPlanDto>>,GetStudyPlansQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetMaterialsQuery, IReadOnlyCollection<MaterialDto>>,GetMaterialsQueryHandler>(); builder.Services.AddScoped<IQueryHandler<GetDueStudySessionsQuery, IReadOnlyCollection<StudySessionDto>>, GetDueStudySessionsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetStudyProgressQuery, StudyProgressDto?>, GetStudyProgressQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetMobileDashboardQuery, MobileDashboardDto>, GetMobileDashboardQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetNextStudySessionQuery, NextStudySessionDto?>, GetNextStudySessionQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetMobileProgressSummaryQuery, MobileProgressSummaryDto>, GetMobileProgressSummaryQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetStudySessionByIdQuery, StudySessionDetailDto>, GetStudySessionByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetMaterialByIdQuery, MaterialDto?>,GetMaterialByIdQueryHandler>();
builder.Services.AddScoped<IStudyScheduleEngine, StudyScheduleEngine>();
builder.Services.AddScoped<IQueryHandler<GetMaterialChunksQuery, IReadOnlyCollection<MaterialChunkDto>>,GetMaterialChunksQueryHandler>();

builder.Services.Decorate(typeof(ICommandHandler<,>), typeof(ValidatedCommandHandler<,>));
builder.Services.Decorate(typeof(IQueryHandler<,>), typeof(ValidatedQueryHandler<,>));

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FlutterWeb");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MentoraXDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
