using GenerativeAI;
// এটিই যথেষ্ট, আলাদা করে using GenerativeAI; লাগবে না
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace whstore.Services
{
    public class GeminiService
    {
        private readonly string _apiKey;

        public GeminiService(IConfiguration configuration)
        {
            // যদি API Key না পাওয়া যায়, তবে এটি নাল বা খালি হতে পারে
            _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        }

        public async Task<string> AskGemini(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "Error: API Key is missing in appsettings.json";
            }

            var model = new GenerativeModel("gemini-1.5-flash", _apiKey);
            var response = await model.GenerateContentAsync(prompt);

            // রেসপন্স নাল কি না চেক করে টেক্সট রিটার্ন করা নিরাপদ
            return response?.Text ?? "No response from Gemini";
        }
    }
}