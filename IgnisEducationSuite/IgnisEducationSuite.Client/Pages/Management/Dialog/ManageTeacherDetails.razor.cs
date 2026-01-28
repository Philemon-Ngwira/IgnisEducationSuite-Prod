using EDUSphereSharedProject.Models;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IgnisEducationSuite.Client.Pages.Management.Dialog
{
    public partial class ManageTeacherDetails
    {
        [Parameter] public Teacher teacher { get; set; } = new();
        [Parameter] public List<Class> subjects { get; set; } = new();
        [Parameter] public List<Class> teachersSubjects { get; set; } = new();
        [Inject] GenericServiceFactory _genericService { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        protected MudTable<Class> _table = new();
        private HashSet<Class> selectedItems = new HashSet<Class>();
        private List<Class> listToSave = new();

        protected async Task UnassignSubject(Class subject)
        {
            var service = _genericService.GetService<Class>();
            subject.TeacherID = null;
            var result = await service.UpdateAsync("api/Dynamic/UpdateEntity", "class", subject);
            if (result.IsSuccess)
            {
                teachersSubjects.Remove(subject);
                subjects.Add(subject);
                Snackbar.Add("Subject Successfully Unassined", Severity.Success);
            }
            else
            {
                Snackbar.Add("Error Saving Error Code: EDUx00000002", Severity.Error);
                return;
            }
        }
        protected async Task FinalizeSubjectSelection()
        {
            var service = _genericService.GetService<List<Class>>();
            foreach (var item in selectedItems)
            {
                Class subject = item as Class;
                if (subject != null)
                {
                    subject.TeacherID = teacher.TeacherID;
                }
                listToSave.Add(item);
            }
            var result = await service.UpdateAsync("api/Dynamic/UpdateEntities", "class", listToSave);
            if (result.IsSuccess)
            {
                foreach (var item in listToSave)
                {

                    teachersSubjects.Add(item);
                    subjects.Remove(item);
                    Snackbar.Add("Subject Successfully Assined", Severity.Success);
                }
            }
            else
            {
                listToSave.Clear();
                Snackbar.Add("Error Saving Error Code: EDUx00000001", Severity.Error);
                return;
            }

        }
    }
}