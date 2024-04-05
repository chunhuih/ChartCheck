using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESAPI = VMS.TPS.Common.Model.API;
using Newtonsoft.Json;
using services.varian.com.AriaWebConnect.Link;
using VMSType = services.varian.com.AriaWebConnect.Common;
using System.Configuration;
using System.Net.Http;

namespace ChartCheck.Core
{
    internal class CheckWorker
    {
        public static void WriteInColor(string s, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write(s);
            Console.ResetColor();
        }
        public static void CheckWorkflowPlan(string MRN, ESAPI.PlanSetup planSetup)
        {
            string apiKey = "8c2b663e-cc05-4e8b-b988-38900d5a3649";
            GetPatientCoursesAndPlanSetupsRequest getPatientCoursesAndPlanSetupsRequest = new GetPatientCoursesAndPlanSetupsRequest
            {
                PatientId = new VMSType.String { Value = MRN },
                TreatmentType = new VMSType.String { Value = "Linac" },
            };
            string request = $"{{\"__type\":\"GetPatientCoursesAndPlanSetupsRequest:http://services.varian.com/AriaWebConnect/Link\", {JsonConvert.SerializeObject(getPatientCoursesAndPlanSetupsRequest).TrimStart('{')}}}";
            string response = SendData(request, true, apiKey);
            GetPatientCoursesAndPlanSetupsResponse getPatientCoursesAndPlanSetupsResponse = JsonConvert.DeserializeObject<GetPatientCoursesAndPlanSetupsResponse>(response);
            foreach (var courses in getPatientCoursesAndPlanSetupsResponse.PatientCourses)
            {
                GetPatientClinicalConceptsRequest getPatientClinicalConceptsRequest = new GetPatientClinicalConceptsRequest
                {
                    PatientId = new VMSType.String { Value = MRN },
                    CourseId = courses.CourseId
                };
                string requestClinicalConcepts = $"{{\"__type\":\"GetPatientClinicalConceptsRequest:http://services.varian.com/AriaWebConnect/Link\", {JsonConvert.SerializeObject(getPatientClinicalConceptsRequest).TrimStart('{')}}}";
                response = SendData(requestClinicalConcepts, true, apiKey);
                if (response.ToLower().Contains("syntax error"))
                {
                    Console.WriteLine("Syntax error was found in the prescription query. Please check prescriptions directly.");
                    break;
                }
                GetPatientClinicalConceptsResponse getPatientClinicalConceptsResponse = JsonConvert.DeserializeObject<GetPatientClinicalConceptsResponse>(response);
                foreach (var concept in getPatientClinicalConceptsResponse.PatientClinicalConcepts)
                {
                    Console.WriteLine($"course: {concept.CourseId.Value} Rx name: {concept.PrescriptionName.Value}");
                    WriteInColor($"Energy: ");
                    WriteInColor($"{concept.Energy.Value}\n", ConsoleColor.Yellow);
                    WriteInColor($"Number of fractions: ");
                    WriteInColor($"{concept.NumberOfFractions.Value} ", ConsoleColor.Yellow);
                    WriteInColor(" frequency: ");
                    WriteInColor($"{concept.Frequency.Value}\n", ConsoleColor.Yellow);
                    foreach (var info in concept.PrescriptionVolumeInfo)
                    {
                        WriteInColor($"Target: ");
                        WriteInColor($"{info.StructureName.Value} ", ConsoleColor.Yellow);
                        WriteInColor($" Dose/fx: ");
                        WriteInColor($"{info.DosePerFraction.Value} Gy", ConsoleColor.Yellow);
                        WriteInColor($" total dose: ");
                        WriteInColor($"{info.TotalDose.Value} Gy\n", ConsoleColor.Yellow);
                    }
                    WriteInColor($"Plans: ");
                    WriteInColor($"{concept.Plans.Value}\n", ConsoleColor.Yellow);
                }
            }
            return;
        }
            public static void CheckTBIPlan(ESAPI.PlanSetup planSetup)
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
        public static void CheckTBIPlanCW(ESAPI.PlanSetup planSetup)
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
        public static void CheckTBITesticularBoost(ESAPI.PlanSetup planSetup)
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
                if (couchAngle != 0)
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
                WriteInColor(String.Format("{0,-30}", "Tolerance:"));
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
        public static void ImageChecks(ESAPI.PlanSetup plan)
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
        public static string SendData(string request, bool bIsJson, string apiKey)
        {
            var sMediaType = bIsJson ? "application/json" : "application/xml";
            var sResponse = System.String.Empty;
            using (var c = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true }))
            {
                if (c.DefaultRequestHeaders.Contains("ApiKey"))
                {
                    c.DefaultRequestHeaders.Remove("ApiKey");
                }
                c.DefaultRequestHeaders.Add("ApiKey", apiKey);
                var task = c.PostAsync(ConfigurationManager.AppSettings["GatewayRestUrl"], new StringContent(request, Encoding.UTF8, sMediaType));
                Task.WaitAll(task);
                var responseTask = task.Result.Content.ReadAsStringAsync();
                Task.WaitAll(responseTask);
                sResponse = responseTask.Result;
            }
            return sResponse;
        }
    }
}
