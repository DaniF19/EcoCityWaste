using EcoCityWaste.Data;
using EcoCityWaste.Models;

namespace EcoCityWaste.Services
{
    public class FailureLogger
    {
        private readonly AppDbContext _context;

        public FailureLogger(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(Exception ex, string controller, string action, string? user)
        {
            try
            {
                var log = new FailureLog
                {
                    Message = ex.Message,
                    StackTrace = ex.ToString(),
                    Controller = controller,
                    Action = action,
                    UserName = user
                };

                _context.FailureLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Avoid crashing even if the logging itself fails
            }
        }
    }
}