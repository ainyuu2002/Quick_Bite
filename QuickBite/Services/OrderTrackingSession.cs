namespace QuickBite.Services;

public static class OrderTrackingSession
{
    private const string TrackedPhoneKey = "TrackedOrderPhone";
    private const string LegacyTrackedOrderIdKey = "TrackedOrderId";

    public static string? GetTrackedPhone(ISession session)
        => session.GetString(TrackedPhoneKey);

    public static void GrantAccess(ISession session, string phone)
    {
        session.SetString(TrackedPhoneKey, phone.Trim());
        session.Remove(LegacyTrackedOrderIdKey);
    }

    public static void ClearAccess(ISession session)
    {
        session.Remove(TrackedPhoneKey);
        session.Remove(LegacyTrackedOrderIdKey);
    }
}
