namespace UnknownCreator.Modules
{
    public interface IHealth
    {
        bool isAlive { get; }
        bool isHurtAfterDeath { get; set; }
        void OnHurt<T>(T data) where T : DamageData, new();
    }
}
