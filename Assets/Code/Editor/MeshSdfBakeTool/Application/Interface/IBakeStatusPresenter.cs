using UnityEngine;

namespace UsefulMesh.Application
{
    public interface IBakeStatusPresenter
    {
        void UpdateProgress(string message, float progress);
        void CompleteBake(string message);
    }
}