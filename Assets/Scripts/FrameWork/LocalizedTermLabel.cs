using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedTermLabel : MonoBehaviour
{
    [SerializeField] private string termKey;
    private TMP_Text label;
    private Term terms;
    private int revision;
    public void Configure(string key) { termKey = key; }

    private async void OnEnable()
    {
        int request = ++revision;
        label = GetComponent<TMP_Text>();
        try
        {
            await GameBootstrap.GetOrCreate().InitializeAsync();
            if (this == null || !isActiveAndEnabled || request != revision) return;
            terms = Term.GetOrCreate();
            terms.LanguageChanged += Refresh;
            Refresh(terms.CurrentLanguage);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { if (this != null) Debug.LogException(exception, this); }
    }

    private void Refresh(string language) { label.text = terms.GetTerm(termKey); }
    private void OnDisable()
    {
        revision++;
        if (terms != null) terms.LanguageChanged -= Refresh;
        terms = null;
    }
}
