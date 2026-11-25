using Evently.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api.Data
{
    public static class Seeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");

            try
            {
                await context.Database.MigrateAsync();

                if (!await context.Users.AnyAsync(u => u.Role == "Admin"))
                {
                    context.Users.Add(new User
                    {
                        Name = "Administrador",
                        Email = "admin@evently.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                        Role = "Admin"
                    });
                }

                if (!await context.Users.AnyAsync(u => u.Role == "Staff"))
                {
                    context.Users.Add(new User
                    {
                        Name = "Portaria",
                        Email = "staff@evently.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                        Role = "Staff"
                    });
                }

                await context.SaveChangesAsync();

                await SeedSampleEventsAsync(context);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Não foi possível aplicar migrações/seed. A base de dados está acessível?");
            }
        }

        private static async Task SeedSampleEventsAsync(AppDbContext context)
        {
            if (await context.Events.AnyAsync(e => e.Title == "Festival de Verão 2026"))
                return;

            var organizer = await context.Users.FirstOrDefaultAsync(u => u.Email == "organizer@evently.com");
            if (organizer == null)
            {
                organizer = new User
                {
                    Name = "Evently Produções",
                    Email = "organizer@evently.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Organizer@123"),
                    Role = "Organizer"
                };
                context.Users.Add(organizer);
                await context.SaveChangesAsync();
            }

            var now = DateTime.UtcNow;

            var samples = new (string Title, string Description, string Category, string Location, int Days, decimal Price, int Total, bool Featured)[]
            {
                ("Festival de Verão 2026", "Três dias de música ao vivo com artistas nacionais e internacionais à beira-mar.", "Música", "Praia de Ipanema, Rio de Janeiro", 21, 180.00m, 2000, true),
                ("Conferência Tech Future", "O maior encontro de tecnologia, IA e inovação do país. Keynotes, workshops e networking.", "Tecnologia", "Centro de Convenções, São Paulo", 35, 350.00m, 1200, true),
                ("Peça de Teatro: O Auto da Barca", "Uma releitura moderna do clássico, aclamada pela crítica.", "Teatro", "Teatro Municipal, Curitiba", 14, 90.00m, 400, false),
                ("Corrida Noturna 10K", "Corrida de rua com percurso iluminado e kit de participante.", "Desporto", "Parque do Ibirapuera, São Paulo", 45, 75.00m, 800, false),
                ("Workshop de Fotografia", "Aprende composição, luz e edição com fotógrafos profissionais.", "Workshop", "Espaço Criativo, Belo Horizonte", 10, 220.00m, 60, false),
                ("Degustação de Vinhos", "Uma noite intimista com harmonização de vinhos e queijos.", "Gastronomia", "Adega Central, Porto Alegre", 28, 150.00m, 80, false)
            };

            foreach (var s in samples)
            {
                var ev = new Event
                {
                    Title = s.Title,
                    Description = s.Description,
                    Date = now.AddDays(s.Days),
                    Location = s.Location,
                    Price = s.Price,
                    TotalTickets = s.Total,
                    Category = s.Category,
                    IsFeatured = s.Featured,
                    Status = "Published",
                    OrganizerId = organizer.Id
                };
                context.Events.Add(ev);
            }

            await context.SaveChangesAsync();

            var events = await context.Events.ToListAsync();

            foreach (var ev in events)
            {
                var half = Math.Max(1, ev.TotalTickets / 2);
                var rest = Math.Max(0, ev.TotalTickets - half);

                context.TicketBatches.Add(new TicketBatch
                {
                    EventId = ev.Id,
                    Name = "Lote 1 - Promocional",
                    Price = ev.Price,
                    Quantity = half,
                    Sold = half / 3
                });

                if (rest > 0)
                {
                    context.TicketBatches.Add(new TicketBatch
                    {
                        EventId = ev.Id,
                        Name = "Lote 2 - Normal",
                        Price = Math.Round(ev.Price * 1.25m, 2),
                        Quantity = rest,
                        Sold = 0
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
