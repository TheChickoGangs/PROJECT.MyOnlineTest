using AptitudeTest.WebApp.Data;
using AptitudeTest.WebApp.Models;
using AptitudeTest.WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;
using System.Runtime.InteropServices;

namespace AptitudeTest.WebApp.Areas.CANDIDATE.Controllers
{
    [Area("CANDIDATE")]
    public class CandidateController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CandidateController(ApplicationDbContext context)
        {
            _context = context;
        }


        //Index
        public IActionResult Index()
        {
            var sessionObj = HttpContext.Session.Get<User>("user");
            if (sessionObj == null)
            {
                return NotFound();
            }

            return View(sessionObj);
        }


        //Profile
        public IActionResult Profile()
        {
            var userProfile = HttpContext.Session.Get<User>("user");
            if (userProfile == null)
            {
                return NotFound();
            }

            return View(userProfile);
        }

        public IActionResult UpdateProfile(int userId)
        {
            var result = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (result == null)
            {
                return NotFound();
            }
            return View(result);
        }

        [HttpPost]
        public IActionResult UpdateProfile(int id, User model)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Update(model);
                _context.SaveChanges();

                TempData["Success"] = "Successfully Update Candidate";
                return RedirectToAction(nameof(Profile));
            }
            else
            {
                return View(model);
            }
        }


        //Ready for Exam
        public IActionResult ReadyForExam()
        {
            var exams = _context.Exams.ToList();

            return View(exams);
        }


        //Attend Exam
        public IActionResult AttendExam(int examId)
        {
            var model = new AttendExamViewModel();
            var userInfo = HttpContext.Session.Get<User>("user");
            var theLastExamId = _context.Exams.Max(e => e.Id);

            if (userInfo != null)
            {
                model.CandidateId = userInfo.Id;
                model.QnAs = new List<QnAViewModel>();
                var getExam = _context.Exams.FirstOrDefault(e => e.Id == examId);
                model.ExamTitle = getExam.Title;
                model.ExamId = examId;
                model.ExamTimer = getExam.Time;

                if (_context.ExamResults.Any(er => er.ExamId == examId && er.UserId == userInfo.Id) == false)
                {
                    model.QnAs = _context.QnAs.Where(q => q.ExamId == examId).Select(x => new QnAViewModel(x)).ToList();
                    model.Message = "";
                }
                else
                {
                    if (examId == theLastExamId)
                    {
                        model.NextExamId = 0;
                    }
                    else
                    {
                        var exams = _context.Exams.ToList();
                        for (var i = 0; i < exams.Count() - 1; i++)
                        {
                            if (exams[i].Id == examId)
                            {
                                model.NextExamId = exams[i + 1].Id;
                            }
                        }
                    }

                    model.Message = "You are already attend this Exam";
                }

                return View(model);
            }
            else
            {
                return RedirectToAction("Login", "Authenticate", new { area = "" });
            }
        }

        [HttpPost]
        public IActionResult AttendExam(AttendExamViewModel vm)
        {
            foreach (var item in vm.QnAs)
            {
                ExamResult examResult = new ExamResult();
                examResult.UserId = vm.CandidateId;
                examResult.QnAId = item.Id;
                examResult.ExamId = item.ExamId;
                examResult.RecordAnswer = item.SelectedAnswer;
                _context.ExamResults.Add(examResult);
            }

            _context.SaveChanges();

            SaveToFinalResult(vm.CandidateId, vm.ExamId);

            var theLastExamId = _context.Exams.Max(e => e.Id);
            if(vm.ExamId == theLastExamId)
            {
                CheckIfPassing(vm.CandidateId);
                return RedirectToAction(nameof(ExitPage));
            }

            return RedirectToAction(nameof(ReadyForExam));
        }

        private void CheckIfPassing(int candidateId)
        {
            if(_context.FinalResults.Where(fr => fr.UserId == candidateId).Any(fr => fr.Status == false))
            {
                return;
            }

            var allFinalResultByCandidate = _context.FinalResults.Include(fr => fr.Exam).Where(fr => fr.UserId == candidateId).ToList();
            var model = new PassingResult();
            model.UserId = candidateId;

            foreach(var item in allFinalResultByCandidate)
            {
                if(item.Exam.Title == "General Knowledge")
                {
                    model.GeneralKnowledgeResult = item.CountCorrect;
                }
                else if(item.Exam.Title == "Mathematics")
                {
                    model.MathematicsResult = item.CountCorrect;
                }
                else
                {
                    model.ComputerTechnologyResult = item.CountCorrect;
                }
            }

            _context.PassingResults.Add(model);
            _context.SaveChanges();
        }

        private void SaveToFinalResult(int candidateId, int examId)
        {
            var model = new FinalResult();
            model.UserId = candidateId;
            model.ExamId = examId;

            var getExamResults = _context.ExamResults.Where(er => er.UserId == candidateId && er.ExamId ==  examId).ToList();
            foreach(var item in getExamResults)
            {
                var getCorrectAnswer = _context.QnAs.FirstOrDefault(q => q.Id == item.Id);
                if(item.RecordAnswer == getCorrectAnswer.TheAnswer)
                {
                    model.CountCorrect++;
                }
            }

            if(model.CountCorrect >= 3)
            {
                model.Status = true;
            }

            _context.FinalResults.Add(model);
            _context.SaveChanges();
        }


        //ExitPage
        public IActionResult ExitPage()
        {
            return View();
        }

        //Show Result 
        public async Task<IActionResult> ShowResult(int candidateId, int examId)
        {
            var model = new FinalResult();

            model.UserId = candidateId;
            model.ExamId = examId;

            var examResult = _context.ExamResults.Where(e => e.UserId == candidateId && e.ExamId == examId).ToList();

            foreach (var item in examResult)
            {
                var getCorrect = _context.QnAs.FirstOrDefault(q => q.Id == item.QnAId);

                if (item.RecordAnswer == getCorrect.TheAnswer)
                {
                    model.CountCorrect++;
                }
            }

            if (model.CountCorrect >= 3)
            {
                model.Status = true;
            }

            _context.FinalResults.Add(model);
            _context.SaveChanges();

            var vm = new FinalResultViewModel();
            vm.ExamInfo = _context.Exams.FirstOrDefault(e => e.Id == examId);
            vm.UserInfo = _context.Users.FirstOrDefault(u => u.Id == candidateId);
            vm.FinalResult = model;
            vm.NextExamId = examId + 1;

            return View(vm);
        }

    }
}
