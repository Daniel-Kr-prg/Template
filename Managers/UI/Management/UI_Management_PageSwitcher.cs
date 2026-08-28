using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class UI_Management_PageSwitcher : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private UI_Elements_Page PageToShowAtStart;

    [Header("Pages")]
    public SerializedDictionary<string, UI_Elements_Page> pages;
    private UI_Elements_Page currentPage;

    [Header("Events")]
    public UnityEvent<UI_Elements_Page> OnPageSwitched;
    public UnityEvent OnAllPagesHidden;

    private void Start()
    {
        foreach (UI_Elements_Page page in pages.Values)
        {
            if (page != null && page != PageToShowAtStart)
            {
                page.Hide(true);
            }
        }

        if (PageToShowAtStart != null && pages.Values.Contains(PageToShowAtStart))
        {
            currentPage = PageToShowAtStart;
            currentPage.Show(true);
        }
    }

    public void Switch(UI_Elements_Page page, bool instantly)
    {
        if (!pages.Values.Contains(page))
        {
            Debug.LogWarning($"[PageSwitcher] No page: {page}");
            return;
        }

        if (currentPage != null)
        {
            currentPage.Hide(instantly);
        }

        currentPage = page;
        currentPage.Show(instantly);

        OnPageSwitched?.Invoke(currentPage);
    }

    public void Switch(UI_Elements_Page page) => Switch(page, false);

    public void Switch(string pageName, bool instantly)
    {
        if (pages.TryGetValue(pageName, out UI_Elements_Page page))
        {
            Switch(page, instantly);
        }
        else
        {
            Debug.LogWarning($"[PageSwitcher] '{pageName}' not found.");
        }
    }

    public void Switch(string pageName) => Switch(pageName, false);

    public void Scroll(int scrollValue, bool instantly = false)
    {
        if (scrollValue == 0 || pages.Count == 0)
            return;

        List<UI_Elements_Page> pageList = pages.Values.ToList();

        if (pageList.Count == 0)
        {
            Debug.LogWarning("[PageSwitcher] No pages in the collection.");
            return;
        }

        int currentIndex = currentPage != null ? pageList.IndexOf(currentPage) : -1;
        int newIndex = (currentIndex + scrollValue) % pageList.Count;

        if (newIndex < 0)
            newIndex += pageList.Count;

        Switch(pageList[newIndex], instantly);
    }

    public void GoToNextPage(bool instantly = false)
    {
        Scroll(1, instantly);
    }

    public void GoToPreviousPage(bool instantly = false)
    {
        Scroll(-1, instantly);
    }

    public void HideAll(bool instantly = false)
    {
        foreach (var page in pages.Values)
        {
            if (page == null) continue;

            if (!page.Hidden)
            {
                page.Hide(instantly);
            }
        }

        currentPage = null;
        OnAllPagesHidden?.Invoke();
    }

    public bool IsPageActive(UI_Elements_Page page)
    {
        return currentPage == page;
    }
}
