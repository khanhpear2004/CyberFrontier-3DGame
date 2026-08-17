using UnityEngine;

public sealed class ObjectiveState
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public int CurrentProgress { get; }
    public int RequiredProgress { get; }
    public Transform WorldTarget { get; }

    public bool HasProgress => RequiredProgress > 0;

    public float NormalizedProgress
    {
        get
        {
            if (!HasProgress)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)CurrentProgress / RequiredProgress);
        }
    }

    public ObjectiveState(string id, string title, string description, int currentProgress, int requiredProgress, Transform worldTarget)
    {
        Id = id;
        Title = title;
        Description = description;
        RequiredProgress = Mathf.Max(0, requiredProgress);
        CurrentProgress = HasProgress ? Mathf.Clamp(currentProgress, 0, RequiredProgress) : 0;
        WorldTarget = worldTarget;
    }

    public ObjectiveState WithProgress(int newProgress)
    {
        return new ObjectiveState(Id, Title, Description, newProgress, RequiredProgress, WorldTarget);
    }

    public ObjectiveState WithWorldTarget(Transform newWorldTarget)
    {
        return new ObjectiveState(Id, Title, Description, CurrentProgress, RequiredProgress, newWorldTarget);
    }
}