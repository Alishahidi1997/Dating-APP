namespace API.Services;

public enum FollowAddResult
{
    Success,
    AlreadyFollowing,
    InvalidTarget,
    DailyLimitReached,
    BlockedUser,
    Failed
}
