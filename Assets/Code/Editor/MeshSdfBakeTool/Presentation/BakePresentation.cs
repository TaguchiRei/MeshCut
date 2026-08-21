using UsefulMesh.Application;

namespace UsefulMesh.Presentation
{
    // Viewが実装すべき要求
    public interface IMeshSdfBakeView
    {
        void DisplayProgress(string message, float progress);
        void ClearProgress(string logMessage);
    }

    public class BakePresenter : IBakeStatusPresenter
    {
        private IMeshSdfBakeView _view;

        // Viewのライフサイクルに合わせてバインド
        public void Bind(IMeshSdfBakeView view) => _view = view;
        public void Unbind() => _view = null;

        public void UpdateProgress(string message, float progress)
        {
            _view?.DisplayProgress(message, progress);
        }

        public void CompleteBake(string message)
        {
            _view?.ClearProgress(message);
        }
    }
}