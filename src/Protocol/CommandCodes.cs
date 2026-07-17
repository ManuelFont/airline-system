namespace Protocol;

public static class CommandCode
{
    //Error
    public const string Error = "000";

    // ── Authentication
    public const string Register = "001";
    public const string Login    = "002";

    // ── Flights
    public const string CreateFlight = "010";
    public const string ModifyFlight = "011";
    public const string DeleteFlight = "012";
    public const string ListFlights  = "013";
    public const string GetFlight    = "014";
    public const string CancelFlight = "015";

    // ── Images
    public const string UploadImageInfo = "020";
    public const string UploadImageData = "021";
    public const string DownloadImage = "022";
    public const string DownloadImageData = "023";

    // ── Tickets
    public const string BuyTicket    = "030";
    public const string CancelTicket = "031";
}
