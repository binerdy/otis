// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;
using Otis.Infrastructure.Storage;

namespace Otis.App.Services;

public static class Strings
{
    private static ResourceMap? map;
    private static ResourceContext? context;

    public static void Initialize(AppLanguage language)
    {
        var tag = language switch
        {
            AppLanguage.English => "en",
            AppLanguage.GermanSwitzerland => "de-CH",
            _ => null,
        };

        var manager = new ResourceManager();
        context = manager.CreateResourceContext();
        if (tag is not null)
        {
            context.QualifierValues["Language"] = tag;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(tag);
        }

        map = manager.MainResourceMap.GetSubtree("Resources");
    }

    public static string Get(string key) => map?.TryGetValue(key, context)?.ValueAsString ?? key;

    public static string Format(string key, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);
}
