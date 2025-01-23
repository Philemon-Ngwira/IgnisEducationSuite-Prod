using EDUSphereSharedProject.AchievementModels;
using IgnisEducationSuite.Client.Pages.Achievements;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IgnisEducationSuite.Client.Pages.Shared
{
    public class DialogEnabler
    {
        [Inject] IDialogService DialogService { get; set; }
        public async Task ShowBadgeDialogs(List<Badge> badges)
        {
            foreach (var badge in badges)
            {
                var parameters = new DialogParameters<AchivementDialog>()
                {
                    {x=>x.badge, badge }
                };

                var options = new DialogOptions()
                {
                    CloseOnEscapeKey = false,
                    BackdropClick = false
                };
                var dialog = DialogService.Show<AchivementDialog>("New Badge", parameters, options);
                var result = await dialog.Result;
            }
        }
    }
}
