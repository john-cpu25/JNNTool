# JNNTool â€” Danh Má»¥c Tá»•ng Há»£p CÃ´ng Cá»¥ (HIá»†N & áº¨N)

> **Cáº­p nháº­t ngÃ y:** 2026-10-17  
> **Vá»‹ trÃ­ file:** `c:\Users\nhann\OneDrive\AI\JNNTool\Tools\UI Manager\JNNTOOL_TOOLS_CATALOG.md`  
> **NguyÃªn táº¯c cá»‘t lÃµi:** Äá»“ng bá»™ 1:1 giá»¯a **Ribbon JNNTool** vÃ  **Command Palette (Dockable Pane)**.

---

## ðŸ“Š Báº£ng Thá»‘ng KÃª Tá»•ng Quan

| Tráº¡ng thÃ¡i | Sá»‘ lÆ°á»£ng | Vá»‹ trÃ­ kiá»ƒm soÃ¡t mÃ£ nguá»“n | Ghi chÃº |
|---|:---:|---|---|
| ðŸŸ¢ **ÄANG HIá»†N** | **21** | [`Core/App.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Core/App.cs) & [`Core/Palette/CommandRegistry.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Core/Palette/CommandRegistry.cs) | Äang hiá»ƒn thá»‹ trá»±c tiáº¿p trÃªn thanh Ribbon vÃ  Command Palette |
| ðŸ”´ **ÄANG áº¨N** | **22** | ÄÆ°á»£c gom gá»n an toÃ n trong khá»‘i `#if false` | ÄÃ£ code sáºµn, sáºµn sÃ ng má»Ÿ láº¡i báº¥t cá»© lÃºc nÃ o |
| âš™ï¸ **ADMIN CLI/WEB**| **2** | `Tools/Admin/` & `scripts/` | Cá»•ng web offline vÃ  script PowerShell cáº¥p key Ä‘á»™c láº­p |
| ðŸš€ **Tá»”NG Cá»˜NG** | **45** | ToÃ n bá»™ dá»± Ã¡n JNNTool | |

---

## ðŸŸ¢ PHáº¦N 1: DANH SÃCH CÃ”NG Cá»¤ ÄANG HIá»†N (21 TOOLS)

ToÃ n bá»™ 21 cÃ´ng cá»¥ dÆ°á»›i Ä‘Ã¢y hiá»ƒn thá»‹ trá»±c tiáº¿p trÃªn thanh Ribbon cá»§a Revit vÃ  Ä‘á»“ng bá»™ xuáº¥t hiá»‡n trong Command Palette:

### 1. Panel `General` (3 cÃ´ng cá»¥)
| STT | TÃªn Ribbon / Palette | Command ID | Icon Enum | Class thá»±c thi | Chá»©c nÄƒng chÃ­nh |
|:---:|---|---|:---:|---|---|
| 1 | **Command Palette** | `cmdPalette` | `CommandPalette` | `JNNTool.Core.Palette.TogglePaletteCmd` | Báº£ng Ä‘iá»u khiá»ƒn tra cá»©u (Ctrl+F) tÃ¬m kiáº¿m nhanh toÃ n bá»™ cÃ´ng cá»¥ JNNTool, há»— trá»£ gÃµ tiáº¿ng Viá»‡t khÃ´ng dáº¥u, ghim lá»‡nh yÃªu thÃ­ch â˜…. |
| 2 | **Account & License** | `cmdLicense` | `Key` | `JNNTool.Tools.Login.Commands.LoginCmd` | Quáº£n lÃ½ báº£n quyá»n, Ä‘Äƒng nháº­p tÃ i khoáº£n, nháº­p mÃ£ key kÃ­ch hoáº¡t mÃ¡y tÃ­nh qua Ä‘Ã¡m mÃ¢y Supabase. |
| 3 | **Ribbon Theme** | `cmdUIManager` | `Palette` | `JNNTool.Tools.UIManager.Commands.UIManagerCmd` | Quáº£n lÃ½ giao diá»‡n, Ä‘á»•i 12 báº£ng mÃ u theme vÃ  chuyá»ƒn Ä‘á»•i 4 phong cÃ¡ch biá»ƒu tÆ°á»£ng (Badge, Glyph, Circle, Outline). |

### 2. Panel `Modeling` (6 cÃ´ng cá»¥ - Bá»‘ cá»¥c 2 Cá»™t x 3 NÃºt nhá» Stacked)
> **Bá»‘ cá»¥c Ribbon:** Gom gá»n thÃ nh **2 cá»™t nÃºt nhá» xáº¿p táº§ng** (Stacked Items 16Ã—16 px), tiáº¿t kiá»‡m 50% diá»‡n tÃ­ch chiá»u ngang thanh Ribbon:
> - **Cá»™t 1:** `Split Beam`, `Create Section`, `Disallow Join Beam`
> - **Cá»™t 2:** `Create Filter`, `Speed Overrider`, `Pile Location`

| STT | TÃªn Ribbon / Palette | Command ID | Icon Enum | Class thá»±c thi | Chá»©c nÄƒng chÃ­nh |
|:---:|---|---|:---:|---|---|
| 4 | **Split Beam** | `cmdSplitBeamByColumn` | `Split` | `JNNTool.SplitBeamByColumnCmd` | Tá»± Ä‘á»™ng ngáº¯t dáº§m thÃ nh tá»«ng Ä‘oáº¡n ngáº¯n táº¡i cÃ¡c vá»‹ trÃ­ giao nhau vá»›i cá»™t hoáº·c dáº§m ngang. |
| 5 | **Create Section** | `cmdCreateSection` | `Section` | `JNNTool.CreateSectionCmd` | Duplicate hÃ ng loáº¡t Type dáº§m/cá»™t vá»›i kÃ­ch thÆ°á»›c tiáº¿t diá»‡n $b \times h$ má»›i siÃªu tá»‘c. |
| 6 | **Disallow Join Beam** | `cmdDisallowJoinBeam` | `DisallowJoin` | `JNNTool.Tools.DisallowJoinBeam.DisallowJoinBeamCmd` | QuÃ©t chá»n nhiá»u dáº§m vÃ  thá»±c hiá»‡n Disallow Join á»Ÿ cáº£ 2 Ä‘áº§u dáº§m chá»‘ng lá»—i tá»± Ä‘á»™ng nháº£y liÃªn káº¿t. |
| 7 | **Create Filter** | `cmdCreateFilter` | `Filter` | `JNNTool.Tools.CreateFilter.Command` | Táº¡o bá»™ lá»c View Filter tá»± Ä‘á»™ng tá»« danh sÃ¡ch pháº§n tá»­ vÃ  gÃ¡n mÃ u sáº¯c trá»±c quan. |
| 8 | **Speed Overrider** | `cmdSpeedOverriderElement` | `Flash` | `JNNTool.Tools.SpeedOverriderElement.Command` | Ghi Ä‘Ã¨ mÃ u sáº¯c Ä‘á»“ há»a vÃ  Ä‘á»™ trong suá»‘t cá»§a pháº§n tá»­ nhanh trong view hiá»‡n hÃ nh. |
| 9 | **Pile Location** | `cmdPileLocation` | `Target` | `JNNTool.Tools.PileLocation.Command` | Chá»n 1 cá»c gá»‘c $\rightarrow$ tool tá»± tÃ¬m táº¥t cáº£ cá»c trong view vÃ  ghi tá»a Ä‘á»™ thá»±c táº¿ ($X, Y$) mm vÃ o tham sá»‘. |
| 10 | **Merge Floor** | `cmdMergeFloorSplit`<br>â€¢ `cmdMergeFloorActiveView`<br>â€¢ `cmdMergeFloorSelect` | `MergeFloor` | `JNNTool.Tools.MergeFloor.MergeFloorCmd`<br>â€¢ `MergeFloorActiveViewCmd`<br>â€¢ `MergeFloorSelectCmd` | *(SplitButton)* Gá»™p cÃ¡c máº£nh sÃ n liá»n ká» (cÃ¹ng tiáº¿t diá»‡n & cao Ä‘á»™) thÃ nh 1 sÃ n duy nháº¥t. Há»— trá»£ **Active View** (tá»± Ä‘á»™ng gá»™p sÃ n gáº§n nhau trong view mÃ  khÃ´ng cáº§n chá»n) vÃ  **Selection** (quÃ©t chá»n tay). |

### 3. Panel `CSI x Revit` (5 cÃ´ng cá»¥ - 1 NÃºt lá»›n + 1 Cá»™t 3 NÃºt nhá» Stacked)
> **Bá»‘ cá»¥c Ribbon:**
> - **NÃºt lá»›n Ä‘á»™c láº­p (32Ã—32 px):** `Import E2K` (lá»‡nh trung tÃ¢m ná»•i báº­t)
> - **1 Cá»™t 3 nÃºt nhá» xáº¿p táº§ng (16Ã—16 px):** `Fit Scope Box`, `Boundary Align`, vÃ  `Auto Join` (SplitButton: Active View / Entire Project)

| STT | TÃªn Ribbon / Palette | Command ID | Icon Enum | Class thá»±c thi | Chá»©c nÄƒng chÃ­nh |
|:---:|---|---|:---:|---|---|
| 10 | **Import E2K** | `cmdImportE2K` | `Cad` | `JNNTool.Tools.CSIxRevit.Commands.ImportE2KCommand` | Chuyá»ƒn Ä‘á»•i mÃ´ hÃ¬nh ETABS/SAP2000 (file `.e2k`, `.$et`) sang cáº¥u kiá»‡n native Revit chuáº©n xÃ¡c. |
| 11 | **Fit Scope Box** | `cmdFitScopeBox` | `ScopeBox` | `JNNTool.Tools.CSIxRevit.Commands.FitScopeBoxCommand` | Cáº¯t khá»›p vÃ  khÃ³a toÃ n bá»™ Ä‘áº§u LÆ°á»›i trá»¥c (Grid) vÃ  Cao Ä‘á»™ (Level) theo Scope Box `JNN_SB`. |
| 12 | **Boundary Align** | `cmdBoundaryAlign` | `Align` | `JNNTool.Tools.CSIxRevit.Commands.BoundaryAlignCommand` | CÄƒn dáº§m/cá»™t biÃªn vÃ o trong ranh Ä‘áº¥t / lÆ°á»›i trá»¥c ($B/2$), tá»± Ä‘á»™ng co/kÃ©o cÃ¡c dáº§m ngang káº¿t ná»‘i vÃ o. |
| 13 | **Auto Join (View)** | `cmdAutoJoinActiveView` | `Join` | `JNNTool.Tools.AutoJoin.AutoJoinActiveViewCommand` | Tá»± Ä‘á»™ng Join vÃ  Ä‘áº£o thá»© tá»± Æ°u tiÃªn ($Cá»™t > Dáº§m > SÃ n$) trong View hiá»‡n hÃ nh *(SplitButton)*. |
| 14 | **Auto Join (Project)** | `cmdAutoJoinProject` | `Join` | `JNNTool.Tools.AutoJoin.AutoJoinProjectCommand` | Tá»± Ä‘á»™ng Join vÃ  Ä‘áº£o thá»© tá»± Æ°u tiÃªn ($Cá»™t > Dáº§m > SÃ n$) cho toÃ n bá»™ mÃ´ hÃ¬nh dá»± Ã¡n *(SplitButton)*. |

### 4. Panel `Layout` (5 cÃ´ng cá»¥)
| STT | TÃªn Ribbon / Palette | Command ID | Icon Enum | Class thá»±c thi | Chá»©c nÄƒng chÃ­nh |
|:---:|---|---|:---:|---|---|
| 15 | **Auto View & Sheet** | `cmdAutoViewSheet` | `ViewSheet` | `RincoNhan.Tools.AutoViewSheet.AutoViewSheetCommand` | Tá»± Ä‘á»™ng sinh View máº·t báº±ng káº¿t cáº¥u / Loading Plan vÃ  tá»± dÃ n vÃ o Sheet theo táº§ng vÃ  Zone. |
| 16 | **Create Views** | `cmdCreateView` | `CreateView` | `JNNTool.Tools.CreateView.Commands.CreateViewCommand` | Tá»± Ä‘á»™ng táº¡o vÃ  nhÃ¢n báº£n nhiá»u View theo Level, gÃ¡n View Template vÃ  nhÃ³m cÃ¢y thÆ° má»¥c Project Browser theo chuáº©n dá»± Ã¡n. |
| 17 | **Dim Grid** | `cmdAutoDimGrid` | `Dim` | `JNNTool.Tools.AutoDimGrid.AutoDimGridCommand` | Tá»± Ä‘á»™ng Dim lÆ°á»›i trá»¥c 4 cáº¡nh (2 hoáº·c 3 lá»›p: TÆ°á»ng / Trá»¥c / Tá»•ng), hoáº·c Dim thá»§ cÃ´ng theo Ä‘iá»ƒm chá»n. |
| 18 | **Dim Col/Wall** | `cmdAutoDimColumn` | `DimColumn` | `JNNTool.Tools.AutoDimColumn.Commands.AutoDimColumnCommand` | Tá»± Ä‘á»™ng ghi kÃ­ch thÆ°á»›c tiáº¿t diá»‡n Cá»™t, VÃ¡ch ($b \times h / L$) vÃ  khoáº£ng cÃ¡ch Ä‘á»‹nh vá»‹ tá»« LÆ°á»›i trá»¥c. |
| 19 | **Element Mark** | `cmdElementMark` | `Mark` | `JNNTool.Tools.ElementMark.ElementMarkCommand` | ÄÃ¡nh sá»‘ hiá»‡u Mark tá»± Ä‘á»™ng cho Dáº§m, Cá»™t, SÃ n theo quy táº¯c dá»± Ã¡n (TrÃ¡i $\rightarrow$ Pháº£i, TrÃªn $\rightarrow$ DÆ°á»›i). |

### 5. Panel `Stair` (2 cÃ´ng cá»¥)
| STT | TÃªn Ribbon / Palette | Command ID | Icon Enum | Class thá»±c thi | Chá»©c nÄƒng chÃ­nh |
|:---:|---|---|:---:|---|---|
| 19 | **Stair Model** | `cmdStairModel` | `Stair` | `JNNTool.Tools.StairModel.Commands.StairModelCommand` | Tá»± Ä‘á»™ng dá»±ng cáº§u thang BTCT (Tháº³ng, Chá»¯ U, Chá»¯ L) gá»“m báº£n thang nghiÃªng, chiáº¿u nghá»‰ vÃ  dáº§m Ä‘á»¡. |
| 20 | **Stair Designer** | `cmdStairDesigner` | `StairDesigner` | `JNN.Stair.Commands.StairDesignerCommand` | Thiáº¿t káº¿ tham sá»‘ cáº§u thang 3D Solid (kÃ¨m báº­c ráº½ quáº¡t Winder) vÃ  tá»± Ä‘á»™ng xuáº¥t náº¡p Family (`.rfa`). |

### 6. Panel `Quantity` (1 cÃ´ng cá»¥)
| STT | TÃªn Ribbon / Palette | Command ID | Icon Enum | Class thá»±c thi | Chá»©c nÄƒng chÃ­nh |
|:---:|---|---|:---:|---|---|
| 21 | **Quantity Takeoff** | `cmdQuantityTakeoff` | `Quantity` | `JNNTool.Tools.QuantityTakeoff.Commands.QuantityTakeoffCommand` | BÃ³c tÃ¡ch khá»‘i lÆ°á»£ng toÃ n diá»‡n (BÃª tÃ´ng, VÃ¡n khuÃ´n, ThÃ©p), táº¡o láº­p tá»©c 3D Section Box vÃ  xuáº¥t file Excel. |

---

## ðŸ”´ PHáº¦N 2: DANH SÃCH CÃ”NG Cá»¤ ÄANG áº¨N (22 TOOLS)

Táº¥t cáº£ cÃ´ng cá»¥ nÃ y Ä‘Æ°á»£c giá»¯ nguyÃªn trong source code (bá»c trong khá»‘i `#if false`) Ä‘á»ƒ phá»¥c vá»¥ má»Ÿ láº¡i khi cáº§n hoáº·c khi hoÃ n thiá»‡n tÃ­nh nÄƒng:

### 1. NhÃ³m Modeling & Dáº§m táº¡m áº©n (7 cÃ´ng cá»¥)
| STT | TÃªn cÃ´ng cá»¥ | Command ID | Class thá»±c thi | LÃ½ do & HÆ°á»›ng má»Ÿ láº¡i |
|:---:|---|---|---|---|
| 1 | **Floor By Room** | `cmdFloorByRoom` | `JNNTool.FloorByRoomCmd` | Tá»± váº½ sÃ n theo Boundary cá»§a Room. Táº¡m áº©n Ä‘á»ƒ tinh gá»n panel Modeling. |
| 2 | **Rebar Beam (Quick)** | `cmdRebarBeam` | `JNNTool.RebarBeamCmd` | Bá»‘ trÃ­ thÃ©p dáº§m báº£n cÅ©. ÄÃ£ cÃ³ phÃ¢n há»‡ Rebar Beam Suite chuyÃªn sÃ¢u thay tháº¿. |
| 3 | **Steel Connection** | `cmdConnection` | `JNNTool.Tools.Connection.Command` | Táº¡o liÃªn káº¿t thÃ©p cho dáº§m cá»™t. Táº¡m áº©n do Ã­t dá»± Ã¡n káº¿t cáº¥u thÃ©p sá»­ dá»¥ng. |
| 4 | **Clash Detection** | `cmdClashDetection` | `JNNTool.Tools.ClashDetection.Command` | Kiá»ƒm tra va cháº¡m mÃ´ hÃ¬nh vá»›i Revit Link. |
| 5 | **Dim Beam** | `cmdCreateDimBeam` | `JNNTool.Tools.CreateDim.CreateDimBeamCmd` | Tá»± táº¡o Dim chiá»u dÃ i tá»•ng 2 Ä‘áº§u dáº§m trÃªn máº·t báº±ng. |
| 6 | **Extend Beam** | `cmdExtendBeam` | `JNNTool.Tools.ExtendBeam.ExtendBeamCmd` | Tá»± nháº­n diá»‡n máº·t gá»‘i á»Ÿ 2 Ä‘áº§u dáº§m vÃ  kÃ©o tim dáº§m tá»›i máº·t gá»‘i chÃ­nh xÃ¡c. |
| 7 | **Rebar Column (Quick)** | `cmdRebarColumn` | `JNNTool.Tools.RebarColumn.Command` | Bá»‘ trÃ­ thÃ©p cá»™t nhanh. ÄÃ£ chuyá»ƒn ngáº§m gom vÃ o phÃ¢n há»‡ Rebar Suite. |

### 2. NhÃ³m CSI x Revit / IFC táº¡m áº©n (2 cÃ´ng cá»¥)
| STT | TÃªn cÃ´ng cá»¥ | Command ID | Class thá»±c thi | LÃ½ do & HÆ°á»›ng má»Ÿ láº¡i |
|:---:|---|---|---|---|
| 8 | **ETABS Converter** | `cmdEtabsConverter` | `JNNTool.Tools.IFCEtabs.Commands.EtabsConverterCommand` | Chuyá»ƒn Ä‘á»•i trá»±c tiáº¿p mÃ´ hÃ¬nh ETABS sang cáº¥u kiá»‡n native kÃ¨m tham sá»‘ IFC (Äang hoÃ n thiá»‡n). |
| 9 | **Property Viewer** | `cmdPropertyViewer` | `JNNTool.Tools.IFCEtabs.Commands.PropertyViewerCommand` | Tra cá»©u thuá»™c tÃ­nh nguá»“n gá»‘c IFC/ETABS cá»§a cáº¥u kiá»‡n. |

### 3. NhÃ³m Stair táº¡m áº©n (1 cÃ´ng cá»¥)
| STT | TÃªn cÃ´ng cá»¥ | Command ID | Class thá»±c thi | LÃ½ do & HÆ°á»›ng má»Ÿ láº¡i |
|:---:|---|---|---|---|
| 10 | **Stair Rebar** | `cmdStairRebarSuiteNew` | `JNN.Stair.Commands.StairDetailingCommand` | Bá»‘ trÃ­ thÃ©p thang Ä‘an chÃ©o chá»‹u kÃ©o gÃ³c lÃµm chá»‘ng bung ná»• bÃª tÃ´ng theo TCVN 5574:2018. |

### 4. PhÃ¢n há»‡ `JNN Rebar Suite` (12 cÃ´ng cá»¥ - áº¨n nguyÃªn Panel)
| STT | TÃªn cÃ´ng cá»¥ | Command ID | Class thá»±c thi | Ná»™i dung tÃ­nh nÄƒng |
|:---:|---|---|---|---|
| 11 | **Rebar Slab** | `cmdSlabRebar` | `JNNTool.RebarSuite.Slab.SlabRebarCommand` | Bá»‘ trÃ­ thÃ©p sÃ n tá»± Ä‘á»™ng: Lá»›p dÆ°á»›i, lá»›p trÃªn vÃ  thÃ©p mÅ© gá»‘i $L/4$ theo TCVN 5574:2018. |
| 12 | **Rebar Beam Suite** | `cmdBeamRebarSuite` | `JNNTool.RebarSuite.Beam.BeamRebarCommand` | Bá»‘ trÃ­ thÃ©p dáº§m liÃªn tá»¥c nhiá»u nhá»‹p: ThÃ©p chá»§, gia cÆ°á»ng gá»‘i, Ä‘ai dÃ y/thÆ°a, tá»± táº¡o MCD/MCN. |
| 13 | **Rebar Column Suite**| `cmdColumnRebarSuite` | `JNNTool.RebarSuite.Column.ColumnRebarCommand` | Bá»‘ trÃ­ thÃ©p chuá»—i cá»™t theo táº§ng: Báº» cá»• chai 1:6 khi Ä‘á»•i tiáº¿t diá»‡n, Ä‘ai AB/CN, tá»± táº¡o MCD. |
| 14 | **Rebar Wall** | `cmdWallRebarSuite` | `JNNTool.RebarSuite.Wall.WallRebarCommand` | Bá»‘ trÃ­ thÃ©p vÃ¡ch 2 lá»›p: ThÃ©p Ä‘á»©ng, ngang, Ä‘ai C ghim biÃªn, chá»‘ng tháº¥m bá»ƒ nÆ°á»›c. |
| 15 | **Rebar Footing** | `cmdFoundationRebarSuite` | `JNNTool.RebarSuite.Foundation.FoundationRebarCommand` | Bá»‘ trÃ­ thÃ©p mÃ³ng Ä‘Æ¡n, mÃ³ng bÄƒng, Ä‘Ã i cá»c: LÆ°á»›i Ä‘Ã¡y mÃ³c uá»‘n 90Â°, thÃ©p chá» cá»™t chÃ¢n vá»‹t. |
| 16 | **Rebar Pile** | `cmdPileRebarSuite` | `JNNTool.RebarSuite.Pile.PileRebarCommand` | Bá»‘ trÃ­ thÃ©p cá»c khoan nhá»“i / cá»c vuÃ´ng: Lá»“ng thÃ©p, Ä‘ai phÃ¢n vÃ¹ng vÃ  xuáº¥t tá»a Ä‘á»™ CSV. |
| 17 | **Rebar Stair Suite** | `cmdStairRebarSuite` | `JNNTool.RebarSuite.Stair.StairRebarCommand` | Bá»‘ trÃ­ thÃ©p báº£n thang chá»‹u nhá»‹p vÃ  thÃ©p neo dáº§m chiáº¿u nghá»‰. |
| 18 | **Rebar Volume** | `cmdRebarVolume` | `JNNTool.RebarSuite.Utilities.Commands.RebarVolumeCommand` | Tiá»‡n Ã­ch: Thá»‘ng kÃª khá»‘i lÆ°á»£ng thÃ©p (CSV) theo Ä‘Æ°á»ng kÃ­nh, cáº¥u kiá»‡n vÃ  táº§ng. |
| 19 | **Rebar Number** | `cmdRebarNumber` | `JNNTool.RebarSuite.Utilities.Commands.RebarNumberCommand` | Tiá»‡n Ã­ch: Tá»± Ä‘á»™ng Ä‘Ã¡nh sá»‘ hiá»‡u thÃ©p (Auto Mark). |
| 20 | **View Unobscured** | `cmdRebarUnobscured` | `JNNTool.RebarSuite.Utilities.Commands.RebarUnobscuredCommand` | Tiá»‡n Ã­ch: Báº­t/táº¯t cháº¿ Ä‘á»™ nhÃ¬n xuyÃªn cá»‘t thÃ©p trong 3D View (Unobscured). |
| 21 | **Isolate Rebar** | `cmdRebarIsolate` | `JNNTool.RebarSuite.Utilities.Commands.IsolateRebarCommand` | Tiá»‡n Ã­ch: CÃ´ láº­p cá»‘t thÃ©p theo cáº¥u kiá»‡n trong view Ä‘ang má»Ÿ. |
| 22 | **Rebar Type Manager**| `cmdRebarTypeManager`| `JNNTool.RebarSuite.Utilities.Commands.RebarTypeManagerCommand` | Tiá»‡n Ã­ch: Táº¡o vÃ  chuáº©n hÃ³a RebarBarType D6â€“D32, HookType, CoverType theo TCVN. |

---

## âš™ï¸ PHáº¦N 3: CÃ”NG Cá»¤ QUáº¢N TRá»Š Äá»˜C Láº¬P (NGOÃ€I RIBBON)

| STT | CÃ´ng cá»¥ | ÄÆ°á»ng dáº«n file | Má»¥c Ä‘Ã­ch sá»­ dá»¥ng |
|:---:|---|---|---|
| 1 | **Admin License Portal** | [`Tools/Admin/admin-license-portal.html`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Tools/Admin/admin-license-portal.html) | Trang web HTML tÄ©nh cháº¡y offline local báº±ng Chrome/Edge: cáº¥p Key má»›i cho khÃ¡ch hÃ ng, xem danh sÃ¡ch user, reset khÃ³a mÃ¡y khi khÃ¡ch Ä‘á»•i thiáº¿t bá»‹, gia háº¡n key. |
| 2 | **Create Key CLI Script** | [`scripts/create-key.ps1`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/scripts/create-key.ps1) | Script PowerShell dÃ²ng lá»‡nh: Táº¡o nhanh mÃ£ key báº£n quyá»n `JNN-XXXX-XXXX-XXXX` tá»± Ä‘á»™ng copy vÃ o Clipboard chá»‰ vá»›i 1 dÃ²ng lá»‡nh. |

---

## ðŸ› ï¸ HÆ°á»›ng Dáº«n KÃ­ch Hoáº¡t / áº¨n Má»™t CÃ´ng Cá»¥

Äá»ƒ báº­t hoáº·c táº¯t báº¥t ká»³ cÃ´ng cá»¥ nÃ o giá»¯a tráº¡ng thÃ¡i **HIá»†N** vÃ  **áº¨N**, báº¡n chá»‰ cáº§n thao tÃ¡c táº¡i 2 file duy nháº¥t:

1. **File Ribbon ([`Core/App.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Core/App.cs)):**
   - ÄÆ°a Ä‘oáº¡n táº¡o `PushButtonData` ra ngoÃ i hoáº·c vÃ o trong khá»‘i `#if false ... #endif`.
2. **File Palette ([`Core/Palette/CommandRegistry.cs`](file:///c:/Users/nhann/OneDrive/AI/JNNTool/Core/Palette/CommandRegistry.cs)):**
   - Di chuyá»ƒn khá»‘i `new ToolCommandInfo(...)` tÆ°Æ¡ng á»©ng ra ngoÃ i hoáº·c vÃ o trong `#if false ... #endif`.
3. **Build láº¡i dá»± Ã¡n:**
   ```powershell
   dotnet build -c Release.R26
   ```
   Command Palette vÃ  Ribbon sáº½ tá»± Ä‘á»™ng cáº­p nháº­t sá»‘ lÆ°á»£ng khá»›p nhau ngay láº­p tá»©c!

