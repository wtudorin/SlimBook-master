using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlimBook.DataAccess.Data;
using SlimBook.DataAccess.Repository;
using SlimBook.DataAccess.Repository.IRepository;
using SlimBook.Utility;
using Stripe;

namespace SlimBook
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
			services.AddSingleton<IEmailSender, EmailSender>();
			services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
			services.Configure<EmailOptions>(Configuration);
			services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));
			services.Configure<TwilioSettings>(Configuration.GetSection("Twilio"));

			services
				.AddIdentity<IdentityUser, IdentityRole>()
				.AddDefaultTokenProviders()
				.AddEntityFrameworkStores<ApplicationDbContext>();

			services.AddScoped<IUnitOfWork, UnitOfWork>();

			services.AddControllersWithViews().AddRazorRuntimeCompilation();
			services.AddRazorPages();

			services.ConfigureApplicationCookie(options =>
			{
				options.LoginPath = "/Identity/Account/Login";
				options.LogoutPath = "/Identity/Account/Logout";
				options.AccessDeniedPath = "/Identity/Account/AccessDenied";
			});
			services.AddAuthentication().AddFacebook(options =>
			{
				options.AppId = "947696522360514";
				options.AppSecret = "7188b26f2c664b8537697f9aacb13041";
			});

			services.AddAuthentication().AddGoogle(options =>
			{
				options.ClientId = "798746108265-2057p3afc0cmo9tk23ge86o20q7chkbn.apps.googleusercontent.com";
				options.ClientSecret = "Ynq25kFE7QBuUOGt-R6kOAld";
			});

			services.AddSession(options =>
			{
				options.IdleTimeout = TimeSpan.FromMinutes(30);
				options.Cookie.HttpOnly = true;
				options.Cookie.IsEssential = true;
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}
			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();
			StripeConfiguration.ApiKey = Configuration.GetSection("Stripe")["SecretKey"];

			app.UseSession();

			app.UseAuthentication();
			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");
				endpoints.MapRazorPages();
			});
		}
	}
}