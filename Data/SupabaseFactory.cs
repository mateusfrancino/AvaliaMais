using Supabase;

namespace Avalia_.Data;

public static class SupabaseFactory
{
    public const string Url = "https://crunxyphmihlbdhlttfr.supabase.co";
    public const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNydW54eXBobWlobGJkaGx0dGZyIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjE5OTkzNDgsImV4cCI6MjA3NzU3NTM0OH0.nT1EPBTgdms1S-rJbD7UMGeKtAdMBtqItHnFe3sVUOw";

    public static Client Create()
    {
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false 
        };
        return new Client(Url, AnonKey, options);
    }
}
