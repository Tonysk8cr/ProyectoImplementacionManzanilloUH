# PBI08 – Consultar el historial de consultas (AB#85)

| Criterio | Implementación | Prueba |

-Presenta las consultas y la fecha de cada una, -Views/Pets/Details.cshtml - History_lists_consultations_most_recent_first
-Permite consultar el detalle, Views/Pets/Consultation.cshtml, GetPetConsultationAsync Patient_consultation_detail_includes_prescriptions_and_addenda |
-Mantiene el orden cronológico, y ClinicalService.GetHistoryAsync (fecha de atención, registro, Id) History_with_equal_dates_is_ordered_by_registration_most_recent_first 
-Distingue consultas de diferentes pacientes,  Filtro por petId; NotFound si la consulta es de otra mascota
-History_only_contains_consultations_of_the_requested_patient, Consultation_of_another_patient_is_not_returned_under_this_patient 



Resultado de `dotnet test` que fue el comando

DOTNET_ROLL_FORWARD=Major dotnet test tests/VetPlatform.Application.Tests --filter "FullyQualifiedName~ClinicalHistory" --logger "console;verbosity=normal"

Restore complete (0.3s)
  VetPlatform.Domain net8.0 succeeded (0.1s) → src/VetPlatform.Domain/bin/Debug/net8.0/VetPlatform.Domain.dll
  VetPlatform.Application net8.0 succeeded (0.0s) → src/VetPlatform.Application/bin/Debug/net8.0/VetPlatform.Application.dll
  VetPlatform.Infrastructure net8.0 succeeded (0.1s) → src/VetPlatform.Infrastructure/bin/Debug/net8.0/VetPlatform.Infrastructure.dll
  VetPlatform.Application.Tests net8.0 succeeded (0.1s) → tests/VetPlatform.Application.Tests/bin/Debug/net8.0/VetPlatform.Application.Tests.dll
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.5.3.1+6b60a9e56a (64-bit .NET 10.0.15)
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.5.3.1+6b60a9e56a (64-bit .NET 10.0.15)
[xUnit.net 00:00:00.05]   Discovering: VetPlatform.Application.Tests
[xUnit.net 00:00:00.05]   Discovering: VetPlatform.Application.Tests
[xUnit.net 00:00:00.07]   Discovered:  VetPlatform.Application.Tests
[xUnit.net 00:00:00.07]   Discovered:  VetPlatform.Application.Tests
[xUnit.net 00:00:00.07]   Starting:    VetPlatform.Application.Tests
[xUnit.net 00:00:00.07]   Starting:    VetPlatform.Application.Tests
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.Patient_consultation_detail_of_unknown_consultation_is_not_found [1 s]
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.History_of_patient_without_consultations_is_empty [42 ms]
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.History_with_equal_dates_is_ordered_by_registration_most_recent_first [66 ms]
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.History_only_contains_consultations_of_the_requested_patient [38 ms]
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.My_patients_excludes_revoked_relationships [41 ms]
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.My_patients_lists_only_pets_with_active_relationship_to_the_vet [36 ms]
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.Vet_without_relationship_cannot_read_patient_consultation_detail [32 ms]
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.Patient_consultation_detail_includes_prescriptions_and_addenda [59 ms]
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.Consultation_of_another_patient_is_not_returned_under_this_patient [37 ms]
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.My_patients_is_forbidden_for_non_vets [31 ms]
[xUnit.net 00:00:01.81]   Finished:    VetPlatform.Application.Tests
[xUnit.net 00:00:01.81]   Finished:    VetPlatform.Application.Tests
  Passed VetPlatform.Application.Tests.ClinicalHistoryTests.History_lists_consultations_most_recent_first [34 ms]

Test Run Successful.
Total tests: 11
     Passed: 11
 Total time: 2.1801 Seconds
  VetPlatform.Application.Tests test net8.0 succeeded (2.3s)

Test summary: total: 11, failed: 0, succeeded: 11, skipped: 0, duration: 2.3s
Build succeeded in 3.3s