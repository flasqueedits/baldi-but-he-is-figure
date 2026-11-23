using UnityEngine;

/// <summary>
/// Added by the scene setup tool to every notebook. Gives each notebook a stable id
/// so all clients agree on which one was collected.
/// </summary>
public class NetworkNotebook : MonoBehaviour
{
    public int id;

    private void Awake()
    {
        NetworkGameState.Register(this);
    }

    public void SetTaken()
    {
        NotebookScript notebook = GetComponent<NotebookScript>();
        if (notebook == null) return;
        notebook.transform.position = new Vector3(notebook.transform.position.x, -20f, notebook.transform.position.z);
        notebook.up = false;
    }

    public void CollectedByLocal()
    {
        NotebookScript notebook = GetComponent<NotebookScript>();
        if (notebook != null) notebook.CollectLocal();
    }
}
