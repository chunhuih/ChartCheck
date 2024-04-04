using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Configuration;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
// [assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]
namespace ChartCheck
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string logName = System.AppDomain.CurrentDomain.FriendlyName + ".log";
            using (StreamWriter w = File.CreateText(logName))
            {
                w.AutoFlush = true;
                string log = "Start of the app: " + System.AppDomain.CurrentDomain.FriendlyName;
                Log(log, w);
            }
            Console.SetWindowSize(120, 60);
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Exception: {e}");
            }
            using (StreamWriter w = File.AppendText(logName))
            {
                w.AutoFlush = true;
                string log = "End of the app: " + System.AppDomain.CurrentDomain.FriendlyName;
                Log(log, w);
            }
        }
        public static void Log(string logMessage, TextWriter w)
        {
            // The $ symbol before the quotation mark creates an interpolated string.
            w.Write($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
            w.WriteLine($": {logMessage}");
        }
        public static void WriteInColor(string s, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write(s);
            Console.ResetColor();
        }
        static void Execute(Application app)
        {
            string logName = System.AppDomain.CurrentDomain.FriendlyName + ".log";
            // Console.Clear();
            WriteInColor("=========================================================================\n", ConsoleColor.Gray);
            WriteInColor("A chart check application by: Chunhui Han.\n", ConsoleColor.Gray);
            WriteInColor("=========================================================================\n", ConsoleColor.Gray);

            string automationPredicate = ConfigurationManager.AppSettings.Get("Automation");
            string mrn;
            if (automationPredicate != null && automationPredicate.ToLower() == "t")
            {
                mrn = ConfigurationManager.AppSettings.Get("MRN");
                Console.WriteLine($"MRN was read from the configuration file: {mrn}");
                CheckThisPatient(mrn, app);
            }
            else
            {
                while (true)
                {
                    WriteInColor("Please enter the patient ID (Press RETURN to exit): ", ConsoleColor.Cyan);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    mrn = Console.ReadLine();
                    Console.ResetColor();
                    if (mrn == "")
                        return;
                    CheckThisPatient(mrn, app);
                    app.ClosePatient();
                }
            }
        }
        static void CheckThisPatient(string mrn, Application app)
        {
            var patient = app.OpenPatientById(mrn);
            if (patient == null)
            {
                WriteInColor("ERROR: This patient ID does not exist.\n", ConsoleColor.Red);
                WriteInColor("Please use a correct patient ID.\n", ConsoleColor.Red);
                return;
            }
            Console.Write("The name of this patient is: ");
            WriteInColor($"{patient.LastName}, {patient.FirstName}", ConsoleColor.Yellow);
            if (patient.MiddleName == string.Empty)
            {
                WriteInColor($"\n", ConsoleColor.Yellow);
            }
            else {
                WriteInColor($" {patient.MiddleName}\n", ConsoleColor.Yellow);
            }
            int nCourses = patient.Courses.Count();
            if (nCourses == 0)
            {
                WriteInColor("ERROR: This patient does not contain any course.\n", ConsoleColor.Red);
                WriteInColor("Please choose another patient with existing courses and run this application again.\n", ConsoleColor.Red);
                return;
            }
            int nPlans = 0;
            foreach (Course eachCourse in patient.Courses)
            {
                nPlans += eachCourse.PlanSetups.Count();
            }
            if (nPlans == 0)
            {
                WriteInColor("ERROR: This patient does not contain any plan.\n", ConsoleColor.Red);
                WriteInColor("Please choose another patient with existing plans and run this application again.\n", ConsoleColor.Red);
                return;
            }
            Console.WriteLine("Please choose a treatment plan from the list below:");
            int index = 0;
            List<string> courseList = new List<string>();
            List<string> planList = new List<string>();
            WriteInColor($"Index {String.Format("{0,-30}", "Course")} " +
                $"{String.Format("{0,-20}", "Prescription")} " +
                $"{String.Format("{0, -20}", "Plan")} Plan approval\n");
            foreach (Course eachCourse in patient.Courses)
            {
                foreach (PlanSetup eachPlan in eachCourse.PlanSetups)
                {
                    courseList.Add(eachCourse.Id);
                    planList.Add(eachPlan.Id);
                    string rxname = "";
                    string planApprovalStatus = PlanSetupApprovalStatus.Unknown.ToString();
                    try
                    {
                        rxname = eachPlan.RTPrescription != null ? eachPlan.RTPrescription.Name : "N/A";
                    }
                    catch
                    {
                        rxname = "N/A";
                    }
                    try
                    {
                        planApprovalStatus = eachPlan.ApprovalStatus.ToString();
                    }
                    catch
                    {
                        planApprovalStatus = "N/A: Workflow plan";
                    }
                    var color = ConsoleColor.Yellow;
                    if (rxname == "N/A" || planApprovalStatus == "Rejected" || 
                        planApprovalStatus == PlanSetupApprovalStatus.Completed.ToString() ||
                        planApprovalStatus == PlanSetupApprovalStatus.CompletedEarly.ToString())
                    {
                        color = ConsoleColor.Red;
                    }
                    WriteInColor($"{String.Format("{0,-5}", $"{index}")} " +
                        $"{String.Format("{0,-30}", $"{courseList[index]} ({eachCourse.ClinicalStatus})")} " +
                        $"{String.Format("{0,-20}", $"{rxname}")} " +
                        $"{String.Format("{0,-20}", $"{planList[index]}")} " +
                        $"{planApprovalStatus}\n", color);
                    index++;
                }
            }
            Console.Write("First please enter the index listed above for your plan to check: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            string indx_selected = Console.ReadLine();
            Console.ResetColor();
            try
            {
                index = Int32.Parse(indx_selected);
            }
            catch (FormatException)
            {
                WriteInColor($"Input error.\n", ConsoleColor.Red);
                return;
            }
            if (index < 0 || index >= courseList.Count())
            {
                WriteInColor($"Input error: index out of bound.\n", ConsoleColor.Red);
                return;
            }
            var courses = patient.Courses.Where(c => c.Id == courseList[index]);
            if (!courses.Any())
            {
                Console.WriteLine("ERROR: The course ID is not found.");
                Console.WriteLine("Please choose a patient with a course and run this application again.");
                return;
            }
            var course = courses.Single();
            var plans = course.PlanSetups.Where(p => p.Id == planList[index]);
            if (!plans.Any())
            {
                Console.WriteLine("ERROR: This plan ID is not found. Program will exit.");
                Console.WriteLine("Please choose a patient with an external beam plan and run this application again.");
                return;
            }
            var planSetup = plans.Single();  // It will throw an exception if there is not exactly one instance.
            Console.Write("Checking plan ");
            WriteInColor($"{planSetup.Id}", ConsoleColor.Yellow);
            Console.Write(" in course ");
            WriteInColor($"{course.Id}\n", ConsoleColor.Yellow);
            bool isWorkflowPlan = false;
            try
            {
                var pType = planSetup.PlanType;
            }
            catch
            {
                isWorkflowPlan = true;
                WriteInColor("This is a workflow plan. ESAPI cannot check this plan.\n", ConsoleColor.Yellow);
                return;
            }
            if (isWorkflowPlan == false && planSetup.PlanType != PlanType.ExternalBeam)
            {
                Console.WriteLine($"ERROR: The plan type is: {planSetup.PlanType}");
                Console.WriteLine("Please choose an external beam plan and run this application again.");
                return;
            }
            // check if treatment beams are defined.
            int numValidBeams = 0;
            foreach (var beam in planSetup.Beams)
            {
                if (beam.IsSetupField == false)
                {
                    numValidBeams++;
                }
            }
            if (numValidBeams == 0)
            {
                WriteInColor($"Warning: The plan has no treatment beams.\n", ConsoleColor.Yellow);
                ImageChecks(planSetup);
                return;
            }
            // check prescription and plan approval status.
            Console.WriteLine("========= Approval status checks: =========");
            RTPrescription rx = planSetup.RTPrescription;
            if (rx == null)
            {
                WriteInColor("ERROR: The prescription is missing for this plan.\n", ConsoleColor.Red);
            }
            if (rx != null)
            {
                string rxStatus = rx.Status;
                Console.Write($"Prescription status: ");
                if (rxStatus.ToLower() == "approved")
                {
                    WriteInColor($"{rxStatus}. Last modified at {rx.HistoryDateTime} by {rx.HistoryUserDisplayName}\n", ConsoleColor.Green);
                }
                else
                {
                    WriteInColor($"{rxStatus}. Last modified at {rx.HistoryDateTime} by {rx.HistoryUserDisplayName}\n", ConsoleColor.Red);
                }
            }
            PlanSetupApprovalStatus status = planSetup.ApprovalStatus;
            Console.Write("Plan status: ");
            if (status == PlanSetupApprovalStatus.TreatmentApproved)
            {
                WriteInColor($"{status}. Last modified at {planSetup.HistoryDateTime} by {planSetup.HistoryUserDisplayName}\n", ConsoleColor.Green);
            }
            else
            {
                WriteInColor($"{status}. Last modified at {planSetup.HistoryDateTime} by {planSetup.HistoryUserDisplayName}\n", ConsoleColor.Red);
            }
            // check prescription info.
            bool useDIBH = false;
            bool useEEBH = false;
            bool inVivoDosimetry = false;
            Console.WriteLine("========= Prescription checks: =========");
            if (rx != null)
            {
                Console.Write($"Name: ");
                WriteInColor($"{rx.Id}. ", ConsoleColor.Yellow);
                Console.Write("Target(s): ");
                foreach (var target in rx.Targets)
                {
                    WriteInColor($"{target.TargetId} ({target.DosePerFraction}/fx), ", ConsoleColor.Yellow);
                }
                Console.WriteLine("\b\b.");
                IEnumerable<string> rxDose = rx.Energies;
                Console.Write($"Mode:");
                foreach (string s in rx.EnergyModes)
                {
                    WriteInColor($" {s}", ConsoleColor.Yellow);
                }
                Console.Write(". Energies: ");
                foreach (string s in rxDose)
                {
                    WriteInColor($"{s}, ", ConsoleColor.Yellow);
                }
                Console.Write("\b\b. ");
                Console.Write("Plan beam energies: ");
                foreach (var beam in planSetup.Beams)
                {
                    if (beam.IsSetupField == false)
                    {
                        WriteInColor($"{beam.EnergyModeDisplayName}, ", ConsoleColor.Yellow);
                    }
                }
                Console.WriteLine("\b\b.");
                var fx = rx.NumberOfFractions;
                Console.Write($"No. prescribed fractions: ");
                WriteInColor($"{fx}", ConsoleColor.Yellow);
                Console.Write("  vs  No. planned fractions: ");
                WriteInColor($"{planSetup.NumberOfFractions}", ConsoleColor.Yellow);
                if (fx == planSetup.NumberOfFractions)
                {
                    WriteInColor($"\tPlan fraction check passed.\n", ConsoleColor.Green);
                }
                else
                {
                    WriteInColor($"\tERROR: Plan fraction check failed.\n", ConsoleColor.Red);
                }
                // check session info
                // These are sessions that are scheduled in Plan Scheduling workspace.
                int numSessions = planSetup.TreatmentSessions.Count();
                Console.Write($"Number of scheduled sessions: ");
                WriteInColor($"{planSetup.TreatmentSessions.Count()} ", ConsoleColor.Yellow);
                if (numSessions == planSetup.NumberOfFractions)
                {
                    WriteInColor($"\tSession check passed.\n", ConsoleColor.Green);
                    Console.Write($"Sessions: ");
                    for (int i = 0; i < planSetup.TreatmentSessions.Count(); i++)
                    {
                        var color = ConsoleColor.Yellow;
                        if (planSetup.TreatmentSessions.ElementAt(i).Status == TreatmentSessionStatus.Completed)
                        {
                            color = ConsoleColor.Green;
                        }
                        WriteInColor($"{i + 1} {planSetup.TreatmentSessions.ElementAt(i).Status}, ", color);
                    }
                    Console.WriteLine("\b\b.");
                }
                else
                {
                    WriteInColor($"\tERROR: Session check failed.\n", ConsoleColor.Red);
                }
                var notes = rx.Notes;
                if (notes.ToLower().Contains("nanodot") || notes.ToLower().Contains("vivo"))
                {
                    inVivoDosimetry = true;
                }
                if (notes.ToLower().Contains("dibh"))
                {
                    useDIBH = true;
                }
                else if ((notes.ToLower().Contains("bh") || (notes.ToLower().Contains("breath") && notes.ToLower().Contains("hold"))) &&
                    notes.ToLower().Contains("eebh") == false && notes.ToLower().Contains("end exp") == false &&
                    notes.ToLower().Contains("end-exp") == false)
                {
                    useDIBH = true;
                }
                else if (notes.ToLower().Contains("eebh") || rx.Notes.ToLower().Contains("end exp") || rx.Notes.ToLower().Contains("end-exp"))
                {
                    useEEBH = true;
                }
                WriteInColor("Rx notes: ");
                WriteInColor($"{notes}\n", ConsoleColor.Yellow);
                if (inVivoDosimetry)
                {
                    WriteInColor("In vivo dosimetry is requested.   ", ConsoleColor.Yellow);
                    var color = Console.BackgroundColor;
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    WriteInColor("Verify if the In-Vivo Dosimetry physics task exists.\n", ConsoleColor.Magenta);
                    Console.BackgroundColor = color;
                }
            }
            else
            {
                WriteInColor("ERROR: The prescription is missing for this plan.\n", ConsoleColor.Red);
            }
            if (IsConventionalTBI(planSetup))
            {
                CheckTBIPlan(planSetup);
                return;
            }
            if (IsConventionalTBICW(planSetup))
            {
                CheckTBIPlanCW(planSetup);
                return;
            }
            if (IsConventionalTBITesticularBoost(planSetup))
            {
                CheckTBITesticularBoost(planSetup);
                return;
            }
            // If the plan is based on 3D images, check treatment plan settings.
            Console.WriteLine("========= Treatment plan setting checks: =========");
            var calcModel = planSetup.PhotonCalculationModel;
            Console.Write($"Calculation model: ");
            WriteInColor($"{calcModel}\n", ConsoleColor.Yellow);
            Dictionary<string, string> calcOptions = planSetup.PhotonCalculationOptions;
            foreach (var item in calcOptions)
            {
                Console.Write($"{item.Key}\t");
                WriteInColor($"{item.Value}\n", ConsoleColor.Yellow);
            }
            bool useGating = planSetup.UseGating;
            if (planSetup.UseGating)
            {
                WriteInColor("Plan using gating.\n", ConsoleColor.Yellow);
            }
            else
            {
                WriteInColor("Gating is not used.\n", ConsoleColor.Yellow);
            }
            if (planSetup.StructureSet != null)
            {
                string structureSetId = planSetup.StructureSet.Id;
                if (structureSetId.ToLower().Contains("ave") && planSetup.UseGating == false)
                {
                    WriteInColor($"Gating check failed. Structure name: {structureSetId}\n", ConsoleColor.Red);
                }
            }
            Console.WriteLine("========= Tx field checks: =========");
            // check the treatment plan type: VMAT, Conformal ARC, SRS, Field-in-field, etc.
            bool is3D = false;
            bool isARC = false;
            bool isSRSARC = false;
            bool isConfARC = false;
            bool isVMAT = false;
            bool isElectron = false;

            foreach (var beam in planSetup.Beams)
            {
                if (beam.IsSetupField == false)
                {
                    if (beam.Technique.ToString() == "STATIC")
                    {
                        is3D = true;
                    }
                    if (beam.Technique.ToString() == "ARC")
                    {
                        isARC = true;
                    }
                    if (beam.Technique.ToString() == "SRS ARC")
                    {
                        isSRSARC = true;
                    }
                    if (beam.MLCPlanType.ToString() == "VMAT")
                    {
                        isVMAT = true;
                    }
                    if (beam.MLCPlanType.ToString() == "ArcDynamic")
                    {
                        isConfARC = true;
                    }
                    if (beam.EnergyModeDisplayName.Contains("E"))
                    {
                        isElectron = true;
                    }
                }
            }
            if (is3D)
            {
                WriteInColor($"3D plan.\n", ConsoleColor.Yellow);
            }
            if (isARC)
            {
                WriteInColor($"ARC - ", ConsoleColor.Yellow);
                if (isVMAT)
                {
                    WriteInColor($"VMAT plan.\n", ConsoleColor.Yellow);
                    // check if arc rotations 
                }
                if (isConfARC)
                {
                    WriteInColor($"Conformal arc plan.\n", ConsoleColor.Yellow);
                }
            }
            if (isSRSARC)
            {
                WriteInColor($"SRS ARC - ", ConsoleColor.Yellow);
                if (isVMAT)
                {
                    WriteInColor($"VMAT plan.\n", ConsoleColor.Yellow);
                    // check if arc rotations 
                }
                if (isConfARC)
                {
                    WriteInColor($"Conformal arc plan.\n", ConsoleColor.Yellow);
                }
            }
            if (isARC || isSRSARC)
            {
                // check couch collision
                double startGantryAngle, endGantryAngle, couchAngle;
                foreach (var beam in planSetup.Beams)
                {
                    if (beam.IsSetupField)
                    {
                        continue;
                    }
                    startGantryAngle = beam.ControlPoints.First().GantryAngle;
                    endGantryAngle = beam.ControlPoints.Last().GantryAngle;
                    couchAngle = beam.ControlPoints.First().PatientSupportAngle;  // couch angle defined as IEC 61217
                    if (couchAngle != 0)
                    {
                        couchAngle = 360 - couchAngle;
                    }
                    Console.Write("ID: ");
                    WriteInColor(String.Format("{0,-6}", beam.Id), ConsoleColor.Yellow);
                    Console.Write($"Couch at ");
                    WriteInColor(String.Format("{0,-5}", $"{couchAngle}"), ConsoleColor.Yellow);
                    Console.Write($" gantry range: ");
                    WriteInColor(String.Format("{0,-5}", $"{startGantryAngle}"), ConsoleColor.Yellow);
                    Console.Write($" -- ");
                    WriteInColor(String.Format("{0, -6}", $"{endGantryAngle}"), ConsoleColor.Yellow);
                    if (couchAngle > 5 && couchAngle < 90)
                    {
                        if ((startGantryAngle < 330 && startGantryAngle > 180) || (endGantryAngle < 330 && endGantryAngle > 180))
                        {
                            WriteInColor("ERROR: potential collision hazard.\n", ConsoleColor.Red);
                        }
                        else
                        {
                            WriteInColor("Collision check passed.\n", ConsoleColor.Green);
                        }
                    }
                    else if (couchAngle > 270 && couchAngle < 355)
                    {
                        if ((startGantryAngle < 180 && startGantryAngle > 30) || (endGantryAngle < 180 && endGantryAngle > 30))
                        {
                            WriteInColor("ERROR: potential collision hazard.\n", ConsoleColor.Red);
                        }
                        else
                        {
                            WriteInColor("Collision check passed.\n", ConsoleColor.Green);
                        }
                    }
                    else
                    {
                        WriteInColor("Collision check passed.\n", ConsoleColor.Green);
                    }
                }
            }
            foreach (var beam in planSetup.Beams)
            {
                if (beam.IsSetupField == false)
                {
                    Console.Write($"ID: ");
                    WriteInColor(String.Format("{0,-6}", beam.Id), ConsoleColor.Yellow);
                    Console.Write("Name: ");
                    WriteInColor(String.Format("{0,-28}", $"{beam.Name}"), ConsoleColor.Yellow);
                    double couchAngle = beam.ControlPoints.First().PatientSupportAngle;  // couch angle defined as IEC 61217
                    if (couchAngle != 0)
                    {
                        couchAngle = 360 - couchAngle;
                    }
                    double gantryAngle = beam.ControlPoints.First().GantryAngle;
                    if (is3D && beam.Name.ToLower().Contains("lao") &&
                        (couchAngle >= 350 || couchAngle <= 10))
                    {
                        if (planSetup.TreatmentOrientation == PatientOrientation.HeadFirstSupine)
                        {
                            if (gantryAngle >= 90)
                            {
                                WriteInColor("Name checked failed: \"LAO\".\n", ConsoleColor.Red);
                            }
                        }
                    }
                    if (is3D && beam.Name.ToLower().Contains("lpo") &&
                        (couchAngle >= 350 || couchAngle <= 10))
                    {
                        if (planSetup.TreatmentOrientation == PatientOrientation.HeadFirstSupine)
                        {
                            if (gantryAngle <= 90 || gantryAngle >= 180)
                            {
                                WriteInColor("Name checked failed: \"LPO\".\n", ConsoleColor.Red);
                            }
                        }
                    }
                    if (is3D && beam.Name.ToLower().Contains("rao") &&
                        (couchAngle >= 350 || couchAngle <= 10))
                    {
                        if (planSetup.TreatmentOrientation == PatientOrientation.HeadFirstSupine)
                        {
                            if (gantryAngle <= 270)
                            {
                                WriteInColor("Name checked failed: \"RAO\".\n", ConsoleColor.Red);
                            }
                        }
                    }
                    if (is3D && beam.Name.ToLower().Contains("rpo") &&
                        (couchAngle >= 350 || couchAngle <= 10))
                    {
                        if (planSetup.TreatmentOrientation == PatientOrientation.HeadFirstSupine)
                        {
                            if (gantryAngle >= 270 || gantryAngle <= 180)
                            {
                                WriteInColor("Name checked failed: \"RPO\".\n", ConsoleColor.Red);
                            }
                        }
                    }
                    bool nameCheck = true;
                    if (rx != null && rx.Notes.ToLower().Contains("bolus") && !beam.Name.ToLower().Contains("wb") && !rx.Notes.ToLower().Contains("no bolus"))
                    {
                        WriteInColor("Name checked failed: missing bolus label.\n", ConsoleColor.Red);
                        nameCheck = false;
                    }
                    if (rx != null && (rx.Notes.ToLower().Contains("eebh") &&
                        !beam.Name.ToLower().Contains("eebh")))
                    {
                        WriteInColor("Name checked failed: missing EEBH (end of expiration breath hold) label.\n", ConsoleColor.Red);
                        nameCheck = false;
                    }
                    if (rx != null && useDIBH && !beam.Name.ToLower().Contains("bh"))
                    {
                        WriteInColor("Name checked failed: missing BH label for DIBH treatments.\n", ConsoleColor.Red);
                        nameCheck = false;
                    }
                    if (rx != null && useDIBH && beam.Name.ToLower().Contains("eebh"))
                    {
                        WriteInColor("Name checked failed: using EEBH instead of BH (for deep inspiration breath hold) label.\n", ConsoleColor.Red);
                        nameCheck = false;
                    }
                    if (rx != null && useEEBH && !beam.Name.ToLower().Contains("eebh"))
                    {
                        WriteInColor("Name checked failed: missing EEBH label.\n", ConsoleColor.Red);
                        nameCheck = false;
                    }
                    if (rx != null && (rx.Notes.ToLower().Contains("hyperarc") || rx.Notes.ToLower().Contains("hyper arc")) &&
                        beam.Name.ToLower().Contains("ha") == false)
                    {
                        WriteInColor("Name check failed: missing HA for HyperArc fields.\n", ConsoleColor.Red);
                        nameCheck = false;
                    }
                    if (beam.Name == "")
                    {
                        WriteInColor("Name check failed: empty beam name.\n", ConsoleColor.Red);
                        nameCheck = false;
                    }
                    if (nameCheck)
                    {
                        WriteInColor("Name check passed.\n", ConsoleColor.Green);
                    }
                }
            }
            if(isElectron)
            {
                foreach (var beam in planSetup.Beams)
                {
                    if (beam.IsSetupField == false)
                    {
                        WriteInColor($"Beam energy: ");
                        WriteInColor($"{beam.EnergyModeDisplayName}, ", ConsoleColor.Yellow);
                        WriteInColor($"Applicator: ");
                        WriteInColor($"{beam.Applicator.Id}, ", ConsoleColor.Yellow);
                        WriteInColor($"Tray: ");
                        WriteInColor($"{beam.Trays.First().Id}. ", ConsoleColor.Yellow);
                        var currentBackgroundColor = Console.BackgroundColor;
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        WriteInColor($"Please check cutout FFDA code for the electron beam.\n", ConsoleColor.Magenta);
                        Console.BackgroundColor = currentBackgroundColor;
                    }
                }
            }
            // Check tolerance table settings
            List<string> toleranceTableList = new List<string>();
            foreach (var beam in planSetup.Beams)
            {
                string toleranceTableLabel = beam.ToleranceTableLabel;
                if (toleranceTableList.Contains(toleranceTableLabel) == false)
                {
                    toleranceTableList.Add(toleranceTableLabel);
                }
            }
            if (toleranceTableList.Count == 1)
            {
                Console.Write("Tolerance Table defined for the fields: ");
                foreach (var table in toleranceTableList)
                {
                    WriteInColor(table, ConsoleColor.Yellow);
                }
                Console.WriteLine("");
            }
            else if (toleranceTableList.Count > 1)
            {
                WriteInColor("Error: multiple tolerance table types are defined for the fields: ", ConsoleColor.Red);
                foreach (var table in toleranceTableList)
                {
                    WriteInColor($"{table}\t", ConsoleColor.Red);
                }
                Console.WriteLine("");
            }
            //
            Console.WriteLine("========= Setup field checks: =========");
            Console.Write("Patient orientation: ");
            WriteInColor($"{planSetup.TreatmentOrientation}\n", ConsoleColor.Yellow);
            foreach (var beam in planSetup.Beams)
            {
                if (beam.IsSetupField)
                {
                    bool isOrthogonal = false;
                    Console.Write($"Setup beam: ");
                    WriteInColor(String.Format("{0,-10}", $"{beam.Id} "), ConsoleColor.Yellow);
                    Console.Write($"Gantry angle: ");
                    WriteInColor(String.Format("{0,-5}", $"{beam.ControlPoints[0].GantryAngle} "), ConsoleColor.Yellow);
                    if (beam.Id.ToLower().Contains("ap"))
                    {
                        isOrthogonal = true;
                        if ((planSetup.TreatmentOrientation == PatientOrientation.HeadFirstSupine ||
                            planSetup.TreatmentOrientation == PatientOrientation.FeetFirstSupine) &&
                            beam.ControlPoints[0].GantryAngle != 0)
                        {
                            WriteInColor("Check failed.\n", ConsoleColor.Red);
                        }
                        else if ((planSetup.TreatmentOrientation == PatientOrientation.HeadFirstProne ||
                                planSetup.TreatmentOrientation == PatientOrientation.FeetFirstProne) &&
                                beam.ControlPoints[0].GantryAngle != 180)
                        {
                            WriteInColor("Check failed.\n", ConsoleColor.Red);
                        }
                        else
                        {
                            WriteInColor("Check passed.\n", ConsoleColor.Green);
                        }
                    }
                    if (beam.Id.ToLower().Contains("pa"))
                    {
                        isOrthogonal = true;
                        if ((planSetup.TreatmentOrientation == PatientOrientation.HeadFirstSupine ||
                            planSetup.TreatmentOrientation == PatientOrientation.FeetFirstSupine) &&
                            beam.ControlPoints[0].GantryAngle != 180)
                        {
                            WriteInColor("Check failed.\n", ConsoleColor.Red);
                        }
                        else if ((planSetup.TreatmentOrientation == PatientOrientation.HeadFirstProne ||
                                planSetup.TreatmentOrientation == PatientOrientation.FeetFirstProne) &&
                                beam.ControlPoints[0].GantryAngle != 0)
                        {
                            WriteInColor("Check failed.\n", ConsoleColor.Red);
                        }
                        else
                        {
                            WriteInColor("Check passed.\n", ConsoleColor.Green);
                        }
                    }
                    if (beam.Id.ToLower().Contains("lt") || beam.Id.ToLower().Contains("ll"))
                    {
                        isOrthogonal = true;
                        if ((planSetup.TreatmentOrientation == PatientOrientation.HeadFirstSupine ||
                            planSetup.TreatmentOrientation == PatientOrientation.FeetFirstProne) &&
                            beam.ControlPoints[0].GantryAngle != 90)
                        {
                            WriteInColor("Check failed.\n", ConsoleColor.Red);
                        }
                        else if ((planSetup.TreatmentOrientation == PatientOrientation.HeadFirstProne ||
                                planSetup.TreatmentOrientation == PatientOrientation.FeetFirstSupine) &&
                                beam.ControlPoints[0].GantryAngle != 270)
                        {
                            WriteInColor("Check failed.\n", ConsoleColor.Red);
                        }
                        else
                        {
                            WriteInColor("Check passed.\n", ConsoleColor.Green);
                        }
                    }
                    if (beam.Id.ToLower().Contains("rt") || beam.Id.ToLower().Contains("rl"))
                    {
                        isOrthogonal = true;
                        if ((planSetup.TreatmentOrientation == PatientOrientation.HeadFirstSupine ||
                            planSetup.TreatmentOrientation == PatientOrientation.FeetFirstProne) &&
                            beam.ControlPoints[0].GantryAngle != 270)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Check failed.");
                        }
                        else if ((planSetup.TreatmentOrientation == PatientOrientation.HeadFirstProne ||
                                planSetup.TreatmentOrientation == PatientOrientation.FeetFirstSupine) &&
                                beam.ControlPoints[0].GantryAngle != 90)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Check failed.");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Check passed.");
                        }
                    }
                    if (beam.Id.ToLower().Contains("ct"))
                    {
                        isOrthogonal = true;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Check passed.");
                    }
                    if (isOrthogonal == false)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Non-standard name.");
                    }
                    Console.ResetColor();
                }
            }
            ImageChecks(planSetup);
            Console.WriteLine("========= Completion of checks =========\n");
        }
        static bool IsConventionalTBI(PlanSetup planSetup)
        {
            if (planSetup.StructureSet == null &&
                (planSetup.Id.ToLower() == "tbi" ||
                planSetup.Id.ToLower().Contains("tbi body") ||
                planSetup.Id.ToLower() == "tbi tb-1" ||
                planSetup.Id.ToLower() == "tbi tb1" ||
                planSetup.Id.ToLower() == "tbi tb-stx" ||
                planSetup.Id.ToLower() == "tbi tbx" ||
                planSetup.Id.ToLower() == "tbi body" ||
                planSetup.Id.ToLower() == "ap tbi" ||
                planSetup.Id.ToLower() == "pa tbi"))
                return true;
            return false;
        }
        static bool IsConventionalTBICW(PlanSetup planSetup)
        {
            if (planSetup.StructureSet != null &&
                planSetup.Id.ToLower().Contains("cw") &&
                (planSetup.Id.ToLower().Contains("ap") || planSetup.Id.ToLower().Contains("pa")
                || planSetup.Id.ToLower().Contains("ant") || planSetup.Id.ToLower().Contains("pos")) &&
                (planSetup.Id.ToLower().Contains("l") || planSetup.Id.ToLower().Contains("r")))
                return true;
            return false;
        }
        static bool IsConventionalTBITesticularBoost(PlanSetup planSetup)
        {
            if (planSetup.RTPrescription != null && planSetup.RTPrescription.Id.ToLower() == "testicular boost" && planSetup.Id.ToLower().Contains("testi"))
                return true;
            return false;
        }
        static void ImageChecks(PlanSetup plan)
        {
            Console.WriteLine("========= Plan image checks: =========");
            if (plan.StructureSet != null)
            {
                var image = plan.StructureSet.Image;
                Console.Write($"Slice thickness: ");
                WriteInColor($"{image.ZRes} mm.\t", ConsoleColor.Yellow);
                Console.Write($"3D image ID: ");
                WriteInColor($"{image.Id}.\t", ConsoleColor.Yellow);
                Console.Write($"Structure set ID: ");
                WriteInColor($"{plan.StructureSet.Id}\n", ConsoleColor.Yellow);
                if (image.Id.ToLower().Contains("ave") || image.Id.ToLower().Contains("-") || image.Id.ToLower().Contains("bh"))
                {
                    if (plan.UseGating)
                    {
                        WriteInColor("Gating is used.\n", ConsoleColor.Green);
                    }
                    else
                    {
                        WriteInColor("Gating is not used. Please verify.\n", ConsoleColor.Yellow);
                    }
                }
            }
            else
            {
                WriteInColor("No structure set for this plan.\n");
            }
            return;
        }
        static void CheckTBIPlan(PlanSetup planSetup)
        {
            WriteInColor($"Checking TBI plan: {planSetup.Id}\n");
            foreach (var beam in planSetup.Beams)
            {
                double GantryAngle = beam.ControlPoints.First().GantryAngle;
                double collimatorAngle = beam.ControlPoints.First().CollimatorAngle;
                double couchAngle = beam.ControlPoints.First().PatientSupportAngle;  // couch angle defined as IEC 61217
                double jawX1 = beam.ControlPoints.First().JawPositions.X1;
                double jawX2 = beam.ControlPoints.First().JawPositions.X2;
                double jawY1 = beam.ControlPoints.First().JawPositions.Y1;
                double jawY2 = beam.ControlPoints.First().JawPositions.Y2;
                WriteInColor($"Beam \"{beam.Id}\" \"{beam.Name}\":\n");
                if (beam.ControlPoints.Count() > 2)
                {
                    WriteInColor("ERROR: not a static field. \n", ConsoleColor.Red);
                }
                WriteInColor($"\tTreatment unit: {beam.TreatmentUnit.Id}\t");
                if (beam.TreatmentUnit.Id != "TrueBeam1" && beam.TreatmentUnit.Id != "TrueBeamSTX")
                {
                    WriteInColor($"ERROR: wrong unit.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tEnergy: {beam.EnergyModeDisplayName}\t");
                if (beam.EnergyModeDisplayName != "10X")
                {
                    WriteInColor($"ERROR: Beam energy {beam.EnergyModeDisplayName}\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tGantry: {GantryAngle}\t");
                if (beam.TreatmentUnit.Id == "TrueBeam1" && (GantryAngle > 280 || GantryAngle < 265))
                {
                    WriteInColor($"ERROR: Gantry angle invalid: {GantryAngle}\n", ConsoleColor.Red);
                }
                else if (beam.TreatmentUnit.Id == "TrueBeamSTX" && (GantryAngle > 100 || GantryAngle < 85))
                {
                    WriteInColor($"ERROR: Gantry angle invalid: {GantryAngle}\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tCollimator: {collimatorAngle}\t");
                if (beam.TreatmentUnit.Id == "TrueBeam1" && collimatorAngle != 315)
                {
                    WriteInColor($"ERROR: Collimator angle invalid: {collimatorAngle}\n", ConsoleColor.Red);
                }
                else if (beam.TreatmentUnit.Id == "TrueBeamSTX" && collimatorAngle != 135)
                {
                    WriteInColor($"ERROR: Collimator angle invalid: {collimatorAngle}\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tCouch angle: {couchAngle}\t");
                if (couchAngle != 0)
                {
                    WriteInColor($"ERROR: Couch angle is wrong.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tX1: {jawX1} X2: {jawX2} Y1: {jawY1} Y2: {jawY2}\t");
                if (jawX1 > -200 || jawX2 < 200 || jawY1 > -200 || jawY2 < 200)
                {
                    WriteInColor($"ERROR: Please check jaw settings: {jawX1} {jawX2} {jawY1} {jawY2}\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tSSD: {beam.PlannedSSD} mm\t");
                if (beam.PlannedSSD < 4030 || beam.PlannedSSD > 4270)
                {
                    WriteInColor($"WARNING: SSD = {beam.PlannedSSD} out of range.\n", ConsoleColor.Yellow);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tMU = {beam.Meterset.Value} {beam.Meterset.Unit}\t");
                if (beam.Meterset.Value > 2600 || beam.Meterset.Value < 1800)
                {
                    WriteInColor($"ERROR: MU out of tolerance.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tDose rate: {beam.DoseRate}\t");
                if (beam.DoseRate != 200)
                {
                    WriteInColor($"ERROR: Dose rate is wrong: {beam.DoseRate}\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tTolerance: {beam.ToleranceTableLabel}\t");
                if (beam.ToleranceTableLabel != "TBI photon")
                {
                    WriteInColor($"ERROR: wrong tolerance table.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tTime: {beam.TreatmentTime / 60} minutes.\t");
                if (beam.Meterset.Value / beam.DoseRate * 1.2 > beam.TreatmentTime)
                {
                    WriteInColor($"ERROR: Treatment time out of tolerance.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tTray: {beam.Trays.First().Id}\t");
                if (beam.Trays.First().Id != "CustomBlockTray")
                {
                    WriteInColor($"ERROR: wrong tray.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor($"\tTechnique: {beam.Technique.Id} {beam.Technique.Name}\t");
                if (beam.Technique.Id != "TOTAL")
                {
                    WriteInColor($"ERROR: wrong technique.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
            }
        }
        static void CheckTBIPlanCW(PlanSetup planSetup)
        {
            WriteInColor($"Checking TBI CW boost plan: ");
            WriteInColor($"{planSetup.Id}\n", ConsoleColor.Yellow);
            foreach (var beam in planSetup.Beams)
            {
                double GantryAngle = beam.ControlPoints.First().GantryAngle;
                double collimatorAngle = beam.ControlPoints.First().CollimatorAngle;
                double couchAngle = beam.ControlPoints.First().PatientSupportAngle;  // couch angle defined as IEC 61217
                double jawX1 = beam.ControlPoints.First().JawPositions.X1;
                double jawX2 = beam.ControlPoints.First().JawPositions.X2;
                double jawY1 = beam.ControlPoints.First().JawPositions.Y1;
                double jawY2 = beam.ControlPoints.First().JawPositions.Y2;
                WriteInColor($"Beam ID: ");
                WriteInColor($"{beam.Id} ", ConsoleColor.Yellow);
                WriteInColor($"name: ");
                WriteInColor($"{beam.Name}:\n", ConsoleColor.Yellow);
                if (beam.ControlPoints.Count() > 2)
                {
                    WriteInColor("ERROR: not a static field. \n", ConsoleColor.Red);
                }
                WriteInColor("Treatment unit: ");
                WriteInColor($"{beam.TreatmentUnit.Id}\t", ConsoleColor.Yellow);
                if (beam.TreatmentUnit.Id != "TrueBeam1" && beam.TreatmentUnit.Id != "TrueBeamSTX")
                {
                    WriteInColor($"ERROR: wrong unit.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor("Energy: ");
                WriteInColor($"{beam.EnergyModeDisplayName}\n", ConsoleColor.Yellow);
                WriteInColor("Gantry: ");
                WriteInColor($"{GantryAngle}\t", ConsoleColor.Yellow);
                if (planSetup.Id.ToLower().Contains("ap") || planSetup.Id.ToLower().Contains("ant") ||
                    beam.Name.ToLower().Contains("ap") || beam.Name.ToLower().Contains("ant"))
                {
                    if (GantryAngle != 90)
                    {
                        WriteInColor($"ERROR: wrong gantry angle.\n", ConsoleColor.Red);
                    }
                    else
                    {
                        WriteInColor($"Pass.\n", ConsoleColor.Green);
                    }
                }
                if (planSetup.Id.ToLower().Contains("pa") || planSetup.Id.ToLower().Contains("pos") ||
                    beam.Name.ToLower().Contains("pa") || beam.Name.ToLower().Contains("pos"))
                {
                    if (GantryAngle != 270)
                    {
                        WriteInColor($"ERROR: wrong gantry angle.\n", ConsoleColor.Red);
                    }
                    else
                    {
                        WriteInColor($"Pass.\n", ConsoleColor.Green);
                    }
                }
                WriteInColor("Collimator: ");
                WriteInColor($"{collimatorAngle}\t", ConsoleColor.Yellow);
                if (collimatorAngle != 315)
                {
                    WriteInColor($"ERROR: wrong collimator angle.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor("Couch angle: ");
                WriteInColor($"{couchAngle}\t", ConsoleColor.Yellow);
                if (couchAngle != 0)
                {
                    WriteInColor($"ERROR: wrong couch angle.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor("SSD: ");
                WriteInColor(string.Format("{0:0} mm\t", beam.PlannedSSD), ConsoleColor.Yellow);
                if (beam.PlannedSSD.ToString("n0") != "1000")
                {
                    WriteInColor($"ERROR: wrong SSD.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor("Dose rate: ");
                WriteInColor($"{beam.DoseRate}\t", ConsoleColor.Yellow);
                if (beam.DoseRate != 1000)
                {
                    WriteInColor($"ERROR: wrong dose rate.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor("MU: ");
                WriteInColor($"{beam.Meterset.Value.ToString("n1")} {beam.Meterset.Unit}\n", ConsoleColor.Yellow);
                WriteInColor("Time: ");
                WriteInColor($"{(beam.TreatmentTime / 60).ToString("n1")} minutes.\t", ConsoleColor.Yellow);
                if (beam.Meterset.Value / beam.DoseRate * 1.2 > beam.TreatmentTime)
                {
                    WriteInColor($"ERROR: wrong time limit.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor("Tolerance: ");
                WriteInColor($"{beam.ToleranceTableLabel}\t", ConsoleColor.Yellow);
                if (beam.ToleranceTableLabel != "TBI CW Electron")
                {
                    WriteInColor($"ERROR: wrong tolerance table.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor("Technique: ");
                WriteInColor($"{beam.Technique.Id}\t", ConsoleColor.Yellow);
                if (beam.Technique.Id != "STATIC")
                {
                    WriteInColor($"ERROR: wrong technique.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor("Tray: ");
                WriteInColor($"{beam.Trays.First().Id}\n", ConsoleColor.Yellow);
                WriteInColor("Number of trays: ");
                WriteInColor($"{beam.Trays.Count()}\t", ConsoleColor.Yellow);
                if (beam.Trays.Count() != 1)
                {
                    WriteInColor($"ERROR: > 1 trays defined.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\t", ConsoleColor.Green);
                    var currentBackgroundColor = Console.BackgroundColor;
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    WriteInColor($"Please check cutout FFDA code for the electron beam.\n", ConsoleColor.Magenta);
                    Console.BackgroundColor = currentBackgroundColor;
                }
                WriteInColor("Applicator: ");
                WriteInColor($"{beam.Applicator.Id}\n", ConsoleColor.Yellow);
                WriteInColor($"X1: {jawX1} X2: {jawX2} Y1: {jawY1} Y2: {jawY2}\n");
            }
        }
        static void CheckTBITesticularBoost(PlanSetup planSetup)
        {
            WriteInColor($"Checking TBI testicular boost plan: ");
            WriteInColor($"{planSetup.Id}\n", ConsoleColor.Yellow);
            foreach (var beam in planSetup.Beams)
            {
                double GantryAngle = beam.ControlPoints.First().GantryAngle;
                double collimatorAngle = beam.ControlPoints.First().CollimatorAngle;
                double jawX1 = beam.ControlPoints.First().JawPositions.X1;
                double jawX2 = beam.ControlPoints.First().JawPositions.X2;
                double jawY1 = beam.ControlPoints.First().JawPositions.Y1;
                double jawY2 = beam.ControlPoints.First().JawPositions.Y2;
                double couchLateral = beam.ControlPoints.First().TableTopLateralPosition;
                double couchVertical = beam.ControlPoints.First().TableTopVerticalPosition;
                double couchLongitudinal = beam.ControlPoints.First().TableTopLongitudinalPosition;
                double couchAngle = beam.ControlPoints.First().PatientSupportAngle;  // couch angle defined as IEC 61217
                if(couchAngle != 0)
                {
                    couchAngle = 360 - couchAngle;
                }
                WriteInColor(String.Format("{0,-30}", "Beam:"));
                WriteInColor($"{beam.Id} {beam.Name}:\n", ConsoleColor.Yellow);
                if (beam.ControlPoints.Count() > 2)
                {
                    WriteInColor("ERROR: not a static field. \n", ConsoleColor.Red);
                }
                WriteInColor(String.Format("{0,-30}", "Treatment unit:"));
                WriteInColor($"{beam.TreatmentUnit.Id}\t", ConsoleColor.Yellow);
                if (beam.TreatmentUnit.Id != "TrueBeam1" && beam.TreatmentUnit.Id != "TrueBeamSTX")
                {
                    WriteInColor($"ERROR: wrong unit.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor(String.Format("{0,-30}", "Energy:"));
                WriteInColor($"{beam.EnergyModeDisplayName}\n", ConsoleColor.Yellow);
                WriteInColor(String.Format("{0,-30}", "Gantry:"));
                WriteInColor($"{GantryAngle}\t", ConsoleColor.Yellow);
                if (GantryAngle != 315)
                {
                    WriteInColor($"ERROR: wrong gantry angle.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor(String.Format("{0,-30}", "Collimator:"));
                WriteInColor($"{collimatorAngle}\t", ConsoleColor.Yellow);
                if (collimatorAngle != 0)
                {
                    WriteInColor($"ERROR: wrong collimator angle.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor(String.Format("{0,-30}", "Couch angle:"));
                WriteInColor($"{couchAngle}\t", ConsoleColor.Yellow);
                if (couchAngle != 270)
                {
                    WriteInColor($"ERROR: wrong couch angle.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor(String.Format("{0,-30}", "SSD:"));
                WriteInColor(string.Format("{0:0} mm\t", beam.PlannedSSD), ConsoleColor.Yellow);
                if (beam.PlannedSSD.ToString("n0") != "1000")
                {
                    WriteInColor($"ERROR: wrong SSD.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor(String.Format("{0,-30}", "Dose rate:"));
                WriteInColor($"{beam.DoseRate}\t", ConsoleColor.Yellow);
                if (beam.DoseRate != 1000)
                {
                    WriteInColor($"ERROR: wrong dose rate.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor(String.Format("{0,-30}", "MU:"));
                WriteInColor($"{beam.Meterset.Value.ToString("n1")} {beam.Meterset.Unit}\n", ConsoleColor.Yellow);
                WriteInColor(String.Format("{0,-30}", "Time:"));
                WriteInColor($"{(beam.TreatmentTime / 60).ToString("n1")} minutes.\t", ConsoleColor.Yellow);
                if (beam.Meterset.Value / beam.DoseRate * 1.2 > beam.TreatmentTime)
                {
                    WriteInColor($"ERROR: wrong time limit.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor(String.Format("{0,-30}","Tolerance:"));
                WriteInColor($"{beam.ToleranceTableLabel}\t", ConsoleColor.Yellow);
                if (beam.ToleranceTableLabel != "TBI Testicle E")
                {
                    WriteInColor($"ERROR: wrong tolerance table.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor(String.Format("{0,-30}", "Technique:"));
                WriteInColor($"{beam.Technique.Id}\t", ConsoleColor.Yellow);
                if (beam.Technique.Id != "STATIC")
                {
                    WriteInColor($"ERROR: wrong technique.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor(String.Format("{0,-30}", "Tray:"));
                WriteInColor($"{beam.Trays.First().Id}\t", ConsoleColor.Yellow);
                if (beam.Trays.First().Id != "FFDA(A10+)")
                {
                    WriteInColor($"ERROR: wrong tray.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.\n", ConsoleColor.Green);
                }
                WriteInColor(String.Format("{0,-30}", "Number of trays: "));
                WriteInColor($"{beam.Trays.Count()}\t", ConsoleColor.Yellow);
                if (beam.Trays.Count() != 1)
                {
                    WriteInColor($"ERROR: > 1 trays defined.\n", ConsoleColor.Red);
                }
                else
                {
                    WriteInColor($"Pass.  ", ConsoleColor.Green);
                    var currentBackgroundColor = Console.BackgroundColor;
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    WriteInColor($"Please check cutout FFDA code for the electron beam.\n", ConsoleColor.Magenta);
                    Console.BackgroundColor = currentBackgroundColor;
                }
                WriteInColor(String.Format("{0,-30}", "Applicator:"));
                WriteInColor($"{beam.Applicator.Id}\n", ConsoleColor.Yellow);
                WriteInColor($"X1: {jawX1} X2: {jawX2} Y1: {jawY1} Y2: {jawY2}\n");
            }
        }
    }
}
