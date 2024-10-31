using MongoDB.Driver;
using RealTimeChatApp.API.Interface;

namespace RealTimeChatApp.API.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IMongoClient _client;
        private IClientSessionHandle _session;

        public IChatRepository ChatRepository { get; private set; }
        public IGroupRepository GroupRepository { get; private set; }
        public IMessageRepository MessageRepository { get; private set; }
        public IUserRepository UserRepository { get; private set; }

        public UnitOfWork(IMongoClient client, IChatRepository chatRepository, IGroupRepository groupRepository,
            IMessageRepository messageRepository, IUserRepository userRepository)
        {
            _client = client;
            ChatRepository = chatRepository;
            GroupRepository = groupRepository;
            MessageRepository = messageRepository;
            UserRepository = userRepository;
        }

        public async Task BeginTransactionAsync()
        {
            _session = await _client.StartSessionAsync();
            _session.StartTransaction();
        }

        public async Task CommitTransactionAsync()
        {
            if (_session == null) throw new InvalidOperationException("Transaction has not been started.");
            try
            {
                await _session.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await RollbackTransactionAsync();
                throw;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_session != null)
                await _session.AbortTransactionAsync();
        }

        public Task<int> SaveChangesAsync()
        {
            return Task.FromResult(1);
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
