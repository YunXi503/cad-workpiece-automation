# Engineering implementation log

This log tracks implementation changes; measured original-case results are separate from analytic fixtures and historical evidence. Original inputs remain local.

## 2026-11-08 — Current evidence and full reading-path audit

- Native continuation: the previously blocked host recovered. Same loaded VSTA source and Case A plan were built and executed with editor F5 in a new isolated Part: 3 sketches, 165 finite primitives, 405 valid active constraints and 3 finite surfaces. Native Copy / Paste Special as Result added a scan display copy; repeated sketch/surface readback was unchanged. [Dated receipt](docs/evidence/case-a-native-codisplay-20261108.json) and [two original viewport exports](docs/surface-contact.md#本次-catia-原生同場展示) preserve the new result without rewriting Nov 5 evidence.
- Scan provenance is the observed copy action plus matching native statistics and rounded bounds, not a complete triangle-coordinate certificate. No PLM save/reopen, labor-time measurement or whole-weld engineering acceptance is inferred. Earlier pending entries below retain their historical scope.
- A new depth-one clone of default main completed 9 actual SDK/build/source/history/math steps with exit0, using an explicitly supplied installed .NET 10.0.403 SDK. No original-workpiece data or licensed host entered this reader test. [Clean-reader receipt](docs/evidence/clean-reader-20261108.json).
- Re-ran all 25 minimal methods and added actual observations, criteria and interpretations to every history page; statuses, original decisions and unknown reasons remain. Corrected potentially inflated interpretations of the pre-listed tolerance choices, simplified branch-stop predicate and in-memory resume example. [Actual synthetic outputs](docs/evidence/history-replay-20261108.json).
- Added a problem-to-code/evidence guide, a three-example geometry walk, dated timeline and checks for method IDs/entry lines/statuses/output tables and all 59 historical images. These are documentation/evidence maintenance changes, not new fitting algorithms. Historical plane solver/source mapping and native manual-edit continuity remain incomplete. [Review record](docs/review-full-20261108.md).

## 2026-11-05 — original Case A delivery

- Authorized goal: use original geometry, complete source-aware geometry programs and VSTA editor execution, publish deidentified portfolio without original files.
- Baseline: commit 094f742. Existing public shell classifier assumes near-cap guide positions and the fitter assumes isolated bends; neither is a verified complete original-case route.
- Plan: typed local support evidence -> original triangle/section validation -> reference-offset engineering draft -> general finite G1 line/arc fitting/reduction -> rounding validation -> fingerprint-bound VSTA -> two review rounds -> GitHub main.
- Source/review support domains are explicit input; unknown physical wall/contact identity remains unknown. Reconstructed draft points are never labelled raw observations.
- Success conditions: source identity <=1e-7 mm, finite draft XY <=0.3 mm, rounding movement <=0.05 mm, endpoint/joint and unit tangent difference <=1e-7, actual native readback <=1e-7 mm. Manufacturing fit is a separate, historically unmet condition.
- Initial results were pending; subsequent measured steps are recorded below.

### Source input, first run

- CLI/core built with zero warnings/errors. The first source audit rejected the original reference format `X value Y value Z value`; the old reader only accepted three whitespace-separated numbers.
- Added an explicit labelled-reference parser to the new local-case path, keeping original reference bytes/hashes intact. The legacy fixture parser is unchanged. Source evidence audit is rerun before interpreting any geometry result.

### Source and draft reproduction

- Original STL retained byte-identical (342,369 faces). Re-slicing and reviewed-support validation returned 83 frames: 60 engineering-width candidates, 7 review exclusions, 16 unresolved, 119 source endpoints. Maximum finite source residual 5.684341886080802e-14 mm.
- New reference-offset/natural-spline/3D-resampling implementation matched the independently frozen historical draft: 1,038 / 779 / 1,013 points; maximum XYZ difference 7.11e-15 / 1.21e-13 / 7.35e-14 mm <= unchanged 1e-7. Historical draft was an oracle only, never production input.
- Original supports remain approximate engineering anchors, not certified physical fillet-end points. All 2,830 generated points are derived.

### General fitting and rounding

- Baseline -> reduced primitives: 83->59, 63->41, 81->65; all original/uniform finite XY checks passed 0.3 mm. Rounded maximum XY: 0.29769230404540187 / 0.2941940032187631 / 0.2622031655095236 mm.
- Independent movement upper bounds: 0.008787129314414083 / 0.004677612108471579 / 0.022313970523077323 mm <=0.05. Endpoint/joint and unit tangent checks passed unchanged 1e-7.
- Tradeoff: historical 129 primitives are fewer than new 165, but two historical sides exceed 0.3. No global minimality or overall segment-count improvement is claimed.
- Source-anchor-only 3D contact maximum: 0.279204270483786 / 0.698533564831493 / 1.799857289317415 mm. Last two fail 0.3; actual physical contact band remains unconfirmed.

### Public replay and Windows

- New analytic shell-strip fixture uses radius40.24 and half-width12; an earlier unexecuted radius8.24 draft was rejected during input review because its offset surface folds. No private geometry changed.
- Source acceptance: deterministic fresh-directory replay, 3 rounded sides, 15 actual rejected malformed/tampered inputs, and reused-source identity becomes unresolved.
- Test-harness first run incorrectly passed a stdout/stderr tuple to json.loads; corrected the harness, not the geometry or thresholds. Math test first build used an unavailable MinNormal name; replaced with exact Math.ScaleB(1,-1022).
- Windows CLI/WinForms built without warnings. Existing 7-case suite, 6 arm regressions, STL parser controls and 25 experiments passed. Original Case A viewer opened/painted all three sides; source endpoints and all-frame unresolved coverage remain separate from derived points.
- Figure renderer initially referenced a non-existent top-level sides key; corrected to run.sides/draft.sides. Numerical source outputs unchanged; all three final PNGs visually inspected.

### Linux environment diagnosis

- WHPX failed with unexpected VP exit; retained logs and used TCG. Debian cloud kernel requires virtio block, SCSI CD and network devices: default legacy devices caused missing root/seed/network. Diagnosed missing device support, then booted the unchanged base with a disposable snapshot and all-virtio devices.
- Debian CLI build, math controls, STL parser and reviewed-source acceptance have passed; remaining full regressions/original-case verification are still running. Final receipt will replace this pending status with measured results only.

### VSTA editor, first attempt

- Created an independent C# test library after user completed 3DPassport login. Imported the new public workflow and exact private Case A plan; external .NET Framework C#5 compilation passed.
- First F5 reached DSYVstaDebugHost but reported CATMain not found, consistent with the host's stale blank-project entry. Source contains public static CATMain. No new success receipt was generated. Next diagnostic is editor edit/save and explicit project build to synchronize the fresh host library, without changing geometry or the workflow.

### VSTA source synchronization and capacity

- An actual editor comment edit/save and explicit Build synchronized the new host entry. Next F5 entered CATMain and logged the exact new plan SHA 8ba8142641ee2a5e2ec91a7d56b8d7c0df3bb9e4314e570413dd1227417bb638, then failed ReadPlan before creating CAD: old per-side resource cap was 24 curves, original case needs 59/41/65.
- Single change authorized by the full-case delivery: raise the bounded curve capacity to256 per side, retaining all finite geometry, 0.3/0.05/1e-7 and constraint checks. Prediction: the same plan must pass curve-count preflight; any later native failure is separately diagnosed, not concealed. Original input/plan unchanged.
- Host runtime reports MTA. Record that fact; do not assume VSTA editor entry is STA or reuse external STA evidence as editor evidence.

### VSTA original-case pass

- Capacity-only revision passed with the same input plan SHA. Actual editor Build/F5 produced3 sketches,59/41/65 finite primitives,145/100/160 active valid constraints and3 finite Z[-50,50] extrusions.
- Native maximum residuals:3.2329694477084558e-13 / 6.0291550413456965e-13 / 8.9528384705772623e-13 mm <=1e-7. The workflow re-read the sketches before creating the fingerprint-bound surfaces.
- During execution paused the debugger at the constraint loop and read index63 on the only65-primitive side, confirming progress; continued without changing geometry. Final runtime status=verified, process=DSYVstaDebugHost, entry=editor-catmain, apartment=MTA.
- First viewport export had stale display and no visible geometry; retained privately. CATIA Update then native Reframe/export displayed all3 surfaces. Hid the tree in CATIA and exported an unedited viewport without account/product identifiers. Public receipt contains only whitelisted aggregate fields and hashes.
- New CAD demo does not import or alter the original STL. Local autosave notification was observed; PLM save is not confirmed. Manufacturing contact remains unaccepted.

### Post-run evidence and reader corrections

- Current workflow additionally rejected5 actual external-STA pre-mutation controls: signed-arc reversal, changed plan fingerprint, absent owning-sketch marker, duplicate extrusion attempt and257-curve capacity overflow. All exit1 with expected messages and no success receipts; they are labelled separately from editor F5 proof.
- Full Windows orchestration passed11 steps, including public fixtures, original-case receipt consistency, code/math controls, existing25 experiments and7 Viewer sides. Original STL SHA rechecked unchanged.
- Found a figure-stage error during review: rounded curves were drawn with the pre-round fitting error in the title. Fixed only the plot metrics/labels, recomputed finite distances independently in Python and checked against the C# report within1e-7. Rounded maxima0.297692304045 /0.294194003219 /0.262203165510mm. Moved the shared legend outside axes to avoid covering the lower-side worst point. Geometry unchanged.
- Preliminary reader scores were not accepted while the label error remained. Corrected figures and separate history/source/VSTA entry paths are used for the final two self-review rounds; source data non-publicity remains an explicit limitation.

### Linux verification scopes

- Debian companion run passed CLI build, math controls, STL parser, reviewed-source replay and the seven-case/25-history suite. Its optional six legacy-arm regressions were stopped to release TCG resources for the original-case run; they are not reported as Debian passes. All six have passed on Windows and remain in the required Windows/Ubuntu CI matrix.
- A second disposable Debian snapshot runs the unchanged original private Case A against the same numerical C# source hashes; it has generated3/3 sides and is performing full source/algorithm replay. No original file is sent to GitHub Actions.

### Linux original-case result

- Targeted Debian12/Linux6.1/.NET10.0.403 run finished with PORTFOLIO_CURRENT_EXIT=0 and VERIFIED. Build0 warnings/errors; all3 original-case sides passed independent replay, finite XY, rounding, endpoints/G1 checks. Both disposable guests are stopped; base image was not modified.
- Input fingerprint and numerical C# source hashes match Windows. Counts match59/41/65. Compared scalar metrics agree within4.7178407014403234e-14mm; CAD plan raw byte hashes differ and are retained separately. No bit-identical cross-platform claim or Linux-native CATIA claim.
- Public Linux receipt contains aggregate metrics/source hashes only; original STL/ASC/manifest/CAD plan and complete logs stay local.

## 2026-11-05 — Surface deliverable missing from presentation

- User finding: curves plus an isolated native face screenshot do not show the main surface-fitting deliverable. Previous self-review missed this criterion; its scores are not proof of complete presentation.
- Scope: presentation only. Prediction and falsification checks recorded before coding in the local surface-showcase plan. Original mesh, source manifest, CAD plan, C# core and Macro remain unchanged; no new fitting method or native execution is claimed.
- Added two equal-scale views of the unchanged mesh with the exact native-verified finite surfaces, three worst-anchor local/normal sections, and every one of the119 anchor distances. Source anchors are foreground markers, not an inferred full contact band.
- Independent analytic closest-point maxima:0.2792042704837781 /0.6985335648314922 /1.7998572893174194mm, within7.9e-15mm of the existing C# maxima (comparison limit1e-7). Probe counts43/28/48; over0.3 counts0/3/3; worst framesF27/F63/F74. Height gaps are0 for these probes only.
- Display controls cover finite line endpoint clamping, positive/negative finite arcs, arc endpoint fallback, center ambiguity, competing primitives and both finite-height bounds. Three invalid surface inputs are rejected. New public receipt binds exact plan and raster hashes.
- Initial display had missing glyphs, transparent tessellation seams and a local cube crop. Corrected characters, used solid face colors and viewport-only crop; final drawings visually read again. Display tessellation/ordering is not used for measurement. Actual STL normal sections and analytic feet carry the distance evidence.
- README now leads with surfaces on the actual part. Dedicated surface case distinguishes the redrawn overlay, real unedited CATIA viewport, subset contact checks and still-unaccepted manufacturing contact. Two different self-review routes retain the private-data/authorship limitations.
- Local targeted verification passed: finite-foot/aggregate/image bindings, current Case A native/geometry evidence consistency,43 Markdown pages/295 relative links with0 errors, and public-file audit. Rechecked original mesh SHA and exact CAD-plan SHA unchanged; no diff in Core/Cli/Macro. The new standard-library display controls are included in both Windows/Ubuntu CI and the full local verification entry.

## 2026-11-05 — Requirement coverage and remaining omissions

- Compared the original plan against the public paths and receipts, without new geometry methods. Recorded pre-change criteria and prediction locally. Found the VSTA STA example still referred to arm-output instead of the README reviewed-output, plus ambiguous before-round labels in the Viewer.
- Independently submitted a valid reviewed-source report with one null source-anchor element to the old Viewer. It did not reject cleanly and timed out after12s; retained the input/result locally. Code inspection found unchecked anchor dereferences during painting. This is a report validation defect, not a geometric result.
- Bounded Viewer fix validates nested source/frame and nonempty finite fitting/rounded curves before painting. Rebuilt with0 warnings/errors;9 malformed reports now reject with exit1/no PNG, and7 valid sides paint. The actual Case A lower view shows pre-round error0.2941mm and display:rounded; final rounded0.294194mm remains a separate verification metric.
- Corrected only Viewer labels/layout and documentation; Core/Cli/Macro and original/native geometry receipts are unchanged. Old SVG labels remain pre-round and are explicitly documented. Added an unedited application-rendered Case A Viewer figure.
- Updated current source dataflow/output schema, old-versus-current native status and old Macro receipt wording. Added a requirement/evidence matrix rather than claiming all work complete based on self-review scores.
- Real remaining work: physical contact band and failing transverse offsets, manual-native-edit to revalidated-plan loop, native mesh/surface co-display and persistence evidence, full historical-source mapping, GUI import/rebuild, measured labor benefit and an author-chosen license. No data or results invented to fill these gaps.
- Reopened all3 original Case A sides with the rebuilt Viewer and re-submitted the exact baseline null-anchor report: valid sides painted, the old bad file now rejected before painting. Current geometry/native/image bindings and44 Markdown pages/322 relative links passed; Core/Cli/Macro have no diff.

## 2026-11-06 — Restore the engineer-assisted surface-draft objective

- User clarified that acceptance prioritizes substantially reducing manual work/time and quickly generating usable main-outline surface drafts, especially simplifying weld details that are difficult to machine. Precision is secondary. The recorded2026-10-23 scope already allowed approximation/inference and engineer adjustment; previous summaries incorrectly elevated a strict contact diagnostic to the overall delivery gate.
- Policy change recorded before edits: delivery is assessed by useful shape/weld treatment, remaining manual adjustment, workflow continuity and time/effort. Existing0.3mm diagnostics, algorithm regression thresholds, native results and original files remain unchanged. No new fitting method or native execution.
- README/report now lead with demonstrated automatic work:3 sketches,165 line/arc elements,405 valid constraints and3 finite extrusions. These counts do not establish a labor-time percentage or complete automatic weld removal.
- Added an engineering acceptance decision and an unfilled same-condition effort/time comparison record. Include support preparation, review, edits and failure/rework; use engineers' usual manual baseline rather than an artificially slow method. No timing values or savings fabricated.
- Diagram labels distinguish a0.3mm reference from overall acceptance. Existing source-anchor maxima and prior physicalContactAccepted=false remain recorded. Future priority is engineer usability/weld review and manual-edit continuity, with precision contact certification reserved for a separately defined need.
- Re-rendered the3 surface diagnostic figures and compared every sampled metric, input/plan identity, height interval and old contact flag against the pre-change receipt: unchanged. Current numerical/native bindings and11finite-foot/3invalid-input controls pass;45Markdown pages/336relative links have0errors. Public candidate audit126files passes; Core/Cli/Viewer/Macro/tests have no diff.

## 2026-11-06 — Complete portfolio and desktop workflow update

- Read current project AGENTS/MEMORY/SOURCES/DECISIONS before work; the new memory structure supersedes repeatedly prepending historical notes. Same original Case A and engineer-assisted draft scope. Plan recorded before changes; no new geometry/classification method or tolerance change.
- Single behavioral addition: WinForms invokes existing source fixture/pipeline/verifier on a worker, stores fresh outputs and opens the review. Protected occupied results and source directories; explicit running/failed/numeric/unrounded states; no native CAD invocation or measured-labor-saving claim.
- Initial build rejected variable declarations inside an or-pattern. Corrected dispatch syntax only;0warnings/errors afterward. Initial regression reused a success PNG for a failure expectation; fixed unique capture filenames, not application acceptance.
- A hypothesized doubling-back guide control still passed fitting and verification; hypothesis falsified, input/output retained privately. Did not fabricate a failure or alter Core. After inspecting the explicit unrounded contract, used a non-centimeter straight span control: rounding withheld and one unrounded side is shown while valid engineering geometry remains explicit. Numerical-exception handling is not claimed as a newly executed native/physical result.
- Actual GUI demo/build tests pass: responsive UI messages, four output files byte-identical to CLI, source unchanged, four bad operations rejected, unrounded state explicit. All3 original Case A core output files plus SVG match the previous verified CLI bytes; same CAD-plan SHA. Application figure is unedited DrawToBitmap output.
- Added practical operator guide, technical/code map and complete-update review; all25 method pages explain their current engineering role without altering historical statuses. Re-read3 private2026-11-04 logs:3cases/9sides/217elements; whitelisted aggregates retain saved precision and missing-reference native block. This is historical transcription, not a rerun.
- Manual native-edit revalidation, native mesh/surface persistence evidence, full historical-file mapping, weld engineering acceptance, labor-time comparison and license choice remain explicit. No completion claim is inferred from interface availability or historical log checks.
- Full current Windows orchestration completed13/13steps with exit0: builds, math/source/STL, existing cases/25experiments, aggregate/image bindings, document/public-tree checks, existing Viewer opening and new async workflow. Preserved exact local summary as update-checks-20261106.json. Source/Core/Cli/Macro remain unchanged; original CAD plan remains8ba814...; no new native result or labor-time claim.

## 2026-11-06 — Add selected-point plane fitting and historical surface figures

- User supplied a historical CAD viewport collection and confirmed that the local rectangular planes were automatically fitted by a program from selected points. The document contains18numbered groups/59images; source group10is absent. Groups are not assumed to be parts, unique surfaces or chronological steps.
- Single update scope: documentation and figures. Added the selected-point plane-fitting method alongside the existing source-side-line/G1/finite-extrusion path, with general plane geometry, four region comparisons and full expandable gallery. No historical solver is inferred as PCA/least-squares/RANSAC, and no new public fitting implementation is claimed.
- Inspected all59images individually. Published unchanged embedded PNG bytes with neutral filenames and a document/image SHA mapping. PNG chunks contain no text/EXIF metadata; no visible company/product/account/path/native-model identity. Original Word, mesh, company code and CAD stay local.
- Reconciled the earlier native co-display gap: historical local-plane images do show mesh and planes together; the current public Case A plan still lacks its own native mesh/surface co-display and persistence evidence. Historical images do not substitute for a new same-plan VSTA execution, contact acceptance, stitched surfaces or measured time savings.
- Rendered local Markdown on desktop/mobile, expanded a gallery group and both SVG diagrams; inspected images. Private preview renderer first required a Windows file URL for its ESM import, then an available headless Edge executable; these are preview-tool setup fixes, not product/geometry changes.
- Existing finite-foot controls and surface/Case A aggregate bindings passed. Final Markdown check passed51pages/559relative targets with0errors; public audit passed199files plus the inventory itself. Core/Cli/Viewer/Macro/tests/CI source remain unchanged.
- Final wording review: the user's confirmation establishes automatic plane fitting from selected points, not how the point domain was selected. Removed the unsupported assertion that selection was manual; retained selection-automation and exact-solver limits. Only README, method prose and flow-diagram wording changed; geometry and images remain the same.
