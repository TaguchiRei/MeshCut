public interface IRecyclable
{ 
    int RecycleId { get; set; }
    void OnRecycle();
}