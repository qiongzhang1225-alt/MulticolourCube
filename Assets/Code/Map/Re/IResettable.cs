public interface IResettable
{
    void SaveCheckpointState();
    void ResetToCheckpointState();
}