using System.Threading.Tasks;

namespace CryptoGramBot.Services
{
    public interface IPriceService
    {
        Task<decimal> GetDollarAmount(string baseCcy, decimal btcAmount);
        Task<decimal> GetPrice(string baseCcy, string termsCurrency);
    }
}
