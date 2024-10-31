using RealTimeChatApp.API.Interface;

namespace RealTimeChatApp.API.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IChatRepository ChatRepository { get; }
        IGroupRepository GroupRepository { get; }
        IMessageRepository MessageRepository { get; }
        IUserRepository UserRepository { get; }

        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<int> SaveChangesAsync();

    }
}
