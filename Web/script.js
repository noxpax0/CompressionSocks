const DRAFT_KEY = "compressionPrescriptionDraft.v1";
const DRAFT_VERSION = 1;

const compressionLevels = {
  light: {
    range: "15–20 mmHg",
    name: "Light Compression",
    info: [
      "Minor varicosities",
      "Tired, aching legs",
      "Minor ankle, leg and foot swelling",
      "Post sclerotherapy",
      "DVT prevention when travelling"
    ]
  },
  moderate: {
    range: "20–30 mmHg",
    name: "Moderate Compression",
    info: [
      "Moderate to severe varicosities",
      "Moderate oedema",
      "Post Vein Surgery/Removal",
      "Superficial thrombophlebitis",
      "Farrow Wrap for Moderate Lymphoedema and CVI"
    ]
  },
  ulcer: {
    range: "40+ mmHg",
    name: "High Compression",
    info: [
      "Severe varicosities",
      "Severe oedema",
      "Lymphatic oedema",
      "Management of active ulcers and manifestations of PTS",
      "Chronic venous insufficiency",
      "Postphlebitic syndrome"
    ]
  }
};

const measurementDefinitions = {
  hip: { label: "Hip", diagram: "leg" },
  thigh: { label: "Thigh", diagram: "leg" },
  calf: { label: "Calf", diagram: "leg" },
  ankle: { label: "Ankle", diagram: "leg" },
  biceps: { label: "Biceps", diagram: "arm" },
  elbow: { label: "Elbow", diagram: "arm" },
  wrist: { label: "Wrist", diagram: "arm" },
  armLength: { label: "Wrist to axilla length", diagram: "arm" },
  knuckles: { label: "Knuckle circumference", diagram: "hand" },
  handLength: { label: "Hand length", diagram: "hand" },
  underbust: { label: "Underbust", diagram: "breast" },
  bust: { label: "Full bust", diagram: "breast" }
};

const optionSets = {
  toe: { label: "Toe option", values: ["Open toe", "Closed toe"] },
  handSize: { label: "Size", values: ["Small", "Medium", "Large"] }
};

const colourOptions = [
  { id: "Black", hex: "#111111" },
  { id: "Blue", hex: "#2563eb" },
  { id: "Skin", hex: "#d6ad8b" }
];

const garmentStyles = [
  { id: "knee", label: "Knee", description: "Knee-length compression stocking", image: "assets/styles/Knee.jpg", option: "toe", measurements: ["calf", "ankle"], diagram: "leg" },
  { id: "thigh", label: "Thigh", description: "Thigh-length compression stocking", image: "assets/styles/Thigh.jpg", option: "toe", measurements: ["thigh", "calf", "ankle"], diagram: "leg" },
  { id: "waist", label: "Waist", description: "Waist-high compression tights", image: "assets/styles/Waist.jpg", option: "toe", measurements: ["hip", "thigh", "calf", "ankle"], diagram: "leg" },
  { id: "farrow", label: "Farrow Wrap", description: "Adjustable compression wrap", image: "assets/styles/farrow wrap.jpg", option: null, measurements: ["calf", "ankle"], diagram: "leg" },
  { id: "ulcer", label: "Knee UlcerCARE", description: "Knee ulcer-care system", image: "assets/styles/KneeUlcer.jpg", option: "toe", measurements: ["calf", "ankle"], diagram: "leg" },
  { id: "arm", label: "Arm", description: "Compression arm sleeve", image: "assets/styles/arm.jpg", option: "handSize", measurements: ["biceps", "elbow", "wrist", "armLength"], diagram: "arm" },
  { id: "gauntlet", label: "Gauntlet", description: "Compression hand garment", image: "assets/styles/gauntlet.jpg", option: "handSize", measurements: ["knuckles", "wrist", "handLength"], diagram: "hand" },
  { id: "glove", label: "Glove", description: "Compression glove", image: "assets/styles/glove.jpg", option: "handSize", measurements: ["knuckles", "wrist", "handLength"], diagram: "hand" },
  { id: "breast", label: "Breast Support", description: "Post-surgical breast support", image: "assets/styles/breastGarment.jpg", option: null, measurements: ["underbust", "bust"], diagram: "breast" }
];

const diagrams = {
  leg: {
    label: "Leg measurement locations",
    markup: `<path d="M48 15h34l8 43-9 31 8 137H70l-5-82-5 82H41l8-137-9-31z" fill="#dceee5" stroke="#23835b" stroke-width="2"/>
      <g fill="#123526" font-weight="700"><text x="96" y="67">Hip</text><text x="96" y="108">Thigh</text><text x="96" y="168">Calf</text><text x="96" y="217">Ankle</text></g>
      <g stroke="#23835b"><path d="M44 63h48M43 105h49M42 164h50M40 213h52"/></g>`
  },
  arm: {
    label: "Arm measurement locations for biceps, elbow, wrist, and arm length",
    markup: `<path d="M50 20h30l8 48-15 80-8 67H55l-8-67-15-80z" fill="#dceee5" stroke="#23835b" stroke-width="2"/>
      <g fill="#123526" font-weight="700"><text x="91" y="67">Biceps</text><text x="91" y="113">Elbow</text><text x="91" y="164">Wrist</text><text x="73" y="235">Length</text></g>
      <g stroke="#23835b"><path d="M43 64h44M42 110h46M40 161h50M58 218V35"/></g>`
  },
  hand: {
    label: "Hand measurement locations for knuckles, wrist, and hand length",
    markup: `<path d="M43 205V105c0-9 12-9 12 0V55c0-10 13-10 13 0v48V42c0-10 13-10 13 0v61V57c0-9 12-9 12 0v61l9-24c4-10 17-5 13 5l-16 47v59z" fill="#dceee5" stroke="#23835b" stroke-width="2"/>
      <g fill="#123526" font-weight="700"><text x="5" y="120">Knuckles</text><text x="8" y="202">Wrist</text><text x="98" y="72">Length</text></g>
      <g stroke="#23835b"><path d="M35 117h80M38 198h64M102 48v147"/></g>`
  },
  breast: {
    label: "Breast support measurement locations for full bust and underbust",
    markup: `<path d="M35 42h60v130H35z" fill="#dceee5" stroke="#23835b" stroke-width="2"/>
      <path d="M39 91c10-28 20-28 26 0 6-28 16-28 26 0M39 124c10 22 20 22 26 0 6 22 16 22 26 0" fill="none" stroke="#23835b" stroke-width="2"/>
      <g fill="#123526" font-weight="700"><text x="2" y="88">Full bust</text><text x="1" y="132">Underbust</text></g>
      <g stroke="#23835b"><path d="M30 87h70M30 126h70"/></g>`
  }
};

const $ = selector => document.querySelector(selector);
const garmentById = id => garmentStyles.find(garment => garment.id === id);

let currentGarmentId = "";
let measurementCache = {};
let colourPriority = [];
let modalOpener = null;
let toastTimer = null;
let modalAction = null;
let modalTransitionTimer = null;
let measurementTransitionTimer = null;
let garmentFilterTimer = null;
let garmentOptionTimer = null;
const stepNudgeTimers = new WeakMap();
let lastWorkflowCompletion = { patient: false, measurements: false };
const MOTION_DURATION = 140;

function initializeApp() {
  renderCompressionCards();
  renderGarmentCards();
  renderColourChoices();
  hideMeasurements();
  bindEvents();
  updatePairButtons();
  updateWorkflowStatus();
  offerDraftRestore();
}

function renderColourChoices() {
  const container = $("#colourPriorities");
  colourOptions.forEach(colour => {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "colour-choice";
    button.dataset.colour = colour.id;
    button.setAttribute("aria-pressed", "false");
    button.setAttribute("aria-label", `${colour.id}; not selected`);

    const square = document.createElement("span");
    square.className = "colour-square";
    square.style.backgroundColor = colour.hex;
    square.setAttribute("aria-hidden", "true");
    const copy = document.createElement("span");
    copy.className = "colour-copy";
    const label = document.createElement("span");
    label.className = "colour-label";
    label.textContent = colour.id;
    const status = document.createElement("span");
    status.className = "colour-status";
    status.textContent = "Select";
    copy.append(label, status);
    const rank = document.createElement("span");
    rank.className = "colour-rank";
    rank.setAttribute("aria-hidden", "true");

    button.append(square, copy, rank);
    button.addEventListener("click", () => toggleColourPriority(colour.id));
    container.append(button);
  });
  updateColourChoices();
}

function toggleColourPriority(colourId) {
  const existingIndex = colourPriority.indexOf(colourId);
  if (existingIndex >= 0) {
    colourPriority.splice(existingIndex, 1);
    $("#colourStatus").textContent = `${colourId} removed from the preference order.`;
  } else {
    colourPriority.push(colourId);
    $("#colourStatus").textContent = `${colourId} selected as ${["first", "second", "third"][colourPriority.length - 1]} preference.`;
  }
  updateColourChoices();
}

function updateColourChoices() {
  document.querySelectorAll(".colour-choice").forEach(button => {
    const rank = colourPriority.indexOf(button.dataset.colour) + 1;
    button.setAttribute("aria-pressed", String(rank > 0));
    button.setAttribute("aria-label", rank > 0 ? `${button.dataset.colour}; preference ${rank}` : `${button.dataset.colour}; not selected`);
    button.querySelector(".colour-rank").textContent = rank || "";
    button.querySelector(".colour-status").textContent = rank > 0 ? `${["First", "Second", "Third"][rank - 1]} preference` : "Select";
  });
  renderColourSlots();
  $("#clearColours").hidden = colourPriority.length === 0;
  updateWorkflowStatus();
}

function renderColourSlots() {
  const container = $("#colourSlots");
  container.replaceChildren();
  [0, 1, 2].forEach(index => {
    const colourId = colourPriority[index];
    const slot = document.createElement("button");
    slot.type = "button";
    slot.className = "colour-slot";
    slot.disabled = !colourId;

    const number = document.createElement("span");
    number.className = "colour-slot-number";
    number.textContent = String(index + 1);
    const copy = document.createElement("span");
    copy.className = "colour-slot-copy";
    copy.textContent = colourId || `${["First", "Second", "Third"][index]} preference`;
    slot.append(number, copy);

    if (colourId) {
      const colour = colourOptions.find(option => option.id === colourId);
      const swatch = document.createElement("span");
      swatch.className = "colour-slot-swatch";
      swatch.style.backgroundColor = colour.hex;
      swatch.setAttribute("aria-hidden", "true");
      slot.prepend(swatch);
      slot.classList.add("is-filled");
      slot.setAttribute("aria-label", `Remove ${colourId}, ${["first", "second", "third"][index]} preference`);
      slot.addEventListener("click", () => toggleColourPriority(colourId));
    } else {
      slot.setAttribute("aria-label", `${["First", "Second", "Third"][index]} colour preference is empty`);
    }
    container.append(slot);
  });
}

function clearColourPriorities() {
  colourPriority = [];
  $("#colourStatus").textContent = "Colour preferences cleared.";
  updateColourChoices();
}

function renderCompressionCards() {
  const container = $("#compression");
  container.tabIndex = -1;

  Object.entries(compressionLevels).forEach(([id, level]) => {
    const card = document.createElement("div");
    card.className = "compression-card";
    card.dataset.level = id;

    const choice = document.createElement("label");
    choice.className = "compression-choice";
    const radio = document.createElement("input");
    radio.type = "radio";
    radio.name = "level";
    radio.value = id;
    radio.required = true;
    radio.setAttribute("aria-describedby", "compression-error");
    radio.addEventListener("change", updateCompressionSelection);

    const copy = document.createElement("span");
    copy.className = "compression-copy";
    const range = document.createElement("strong");
    range.textContent = level.range;
    const name = document.createElement("small");
    name.textContent = level.name;
    copy.append(range, name);
    choice.append(radio, copy);

    const infoButton = document.createElement("button");
    infoButton.className = "info";
    infoButton.type = "button";
    infoButton.textContent = "i";
    infoButton.setAttribute("aria-label", `Information about ${level.range} ${level.name}`);
    infoButton.addEventListener("click", () => openCompressionInfo(level));

    card.append(choice, infoButton);
    container.append(card);
  });
}

function updateCompressionSelection(event) {
  document.querySelectorAll(".compression-card").forEach(card => {
    card.classList.toggle("selected", Boolean(card.querySelector("input:checked")));
  });
  clearError("compression");
  updateWorkflowStatus();
  if (event && currentGarmentId) flashNextStep($("#measurementSection"));
}

function openCompressionInfo(level) {
  const list = document.createElement("ul");
  level.info.forEach(item => {
    const li = document.createElement("li");
    li.textContent = item;
    list.append(li);
  });
  openModal(`${level.range} — ${level.name}`, list, [{ label: "Close", action: closeModal }]);
}

function renderGarmentCards() {
  const container = $("#garments");
  container.tabIndex = -1;

  garmentStyles.forEach(garment => {
    const card = document.createElement("article");
    card.className = "garment";
    card.dataset.id = garment.id;

    const button = document.createElement("button");
    button.type = "button";
    button.className = "garment-select";
    button.setAttribute("role", "radio");
    button.setAttribute("aria-checked", "false");
    button.setAttribute("aria-label", `Select ${garment.label}: ${garment.description}`);
    button.addEventListener("click", () => selectGarment(garment.id));

    const imageBox = document.createElement("div");
    imageBox.className = "image";
    const image = document.createElement("img");
    image.src = garment.image;
    image.alt = garment.label;
    image.loading = "lazy";
    image.addEventListener("error", () => {
      const fallback = document.createElement("span");
      fallback.className = "image-fallback";
      fallback.textContent = garment.label;
      imageBox.replaceChildren(fallback);
    });
    imageBox.append(image);

    const copy = document.createElement("div");
    copy.className = "garment-copy";
    const title = document.createElement("strong");
    title.textContent = garment.label;
    const description = document.createElement("small");
    description.textContent = garment.description;
    copy.append(title, description);

    button.append(imageBox, copy);
    card.append(button);
    container.append(card);
  });

  const configPanel = document.createElement("aside");
  configPanel.id = "garmentConfigPanel";
  configPanel.className = "garment-config-panel";
  configPanel.hidden = true;
  container.append(configPanel);
}

function cacheVisibleMeasurements() {
  document.querySelectorAll("#measurementFields input").forEach(input => {
    measurementCache[input.id] = input.value;
  });
}

function selectGarment(id) {
  if (currentGarmentId === id) {
    clearGarmentSelection();
    return;
  }

  cacheVisibleMeasurements();
  clearTimeout(garmentFilterTimer);
  currentGarmentId = id;
  $("#selectionRow").classList.add("has-garment");
  const compressionSection = $("#compressionSection");
  compressionSection.hidden = false;
  compressionSection.classList.remove("is-revealing");
  requestAnimationFrame(() => compressionSection.classList.add("is-revealing"));
  const garment = garmentById(id);

  document.querySelectorAll(".garment[data-id]").forEach(card => {
    const selected = card.dataset.id === id;
    card.classList.toggle("selected", selected);
    card.hidden = false;
    card.classList.toggle("is-filtering-out", !selected);
    card.querySelector(".garment-select").setAttribute("aria-checked", String(selected));
  });
  $("#changeGarment").hidden = false;

  garmentFilterTimer = setTimeout(() => {
    document.querySelectorAll(".garment[data-id]").forEach(card => {
      const selected = card.dataset.id === id;
      card.hidden = !selected;
      card.classList.remove("is-filtering-out");
      if (selected) card.classList.add("is-filtering-in");
    });
    setTimeout(() => document.querySelectorAll(".garment.is-filtering-in").forEach(card => card.classList.remove("is-filtering-in")), MOTION_DURATION);
  }, MOTION_DURATION);

  renderGarmentOption(garment);
  renderMeasurements(garment);
  clearError("garment");
  clearError("measurements");
  updateWorkflowStatus();
  flashNextStep(garment.option ? $("#garmentConfigPanel") : compressionSection);
  requestAnimationFrame(() => {
    const nextControl = garment.option
      ? document.querySelector(`[data-garment-option="${garment.id}"]`)
      : document.querySelector("#measurementFields input");
    nextControl?.focus();
  });
}

function clearGarmentSelection({ cacheMeasurements = true, focusFirst = false } = {}) {
  if (cacheMeasurements) cacheVisibleMeasurements();
  clearTimeout(garmentFilterTimer);
  clearTimeout(garmentOptionTimer);
  currentGarmentId = "";
  $("#compressionSection").hidden = true;
  $("#selectionRow").classList.remove("has-garment");
  document.querySelectorAll(".garment[data-id]").forEach(card => {
    card.hidden = false;
    card.classList.remove("selected", "is-filtering-out");
    card.classList.add("is-filtering-in");
    card.querySelector(".garment-select").setAttribute("aria-checked", "false");
  });
  const configPanel = $("#garmentConfigPanel");
  if (configPanel) {
    configPanel.hidden = true;
    configPanel.replaceChildren();
  }
  $("#changeGarment").hidden = true;
  setTimeout(() => document.querySelectorAll(".garment.is-filtering-in").forEach(card => card.classList.remove("is-filtering-in")), MOTION_DURATION);
  hideMeasurements();
  clearError("garment");
  clearError("measurements");
  updateWorkflowStatus();
  if (focusFirst) requestAnimationFrame(() => document.querySelector('.garment[data-id] .garment-select')?.focus());
}

function renderGarmentOption(garment, selectedValue = "") {
  const container = $("#garmentConfigPanel");
  clearTimeout(garmentOptionTimer);
  container.replaceChildren();
  container.hidden = true;
  const selectedCard = document.querySelector(`.garment[data-id="${garment.id}"]`);
  if (selectedCard) selectedCard.after(container);

  const heading = document.createElement("div");
  heading.className = "garment-config-heading";
  const title = document.createElement("h3");
  title.id = "garmentConfigTitle";
  title.textContent = `Options for ${garment.label}`;
  heading.append(title);
  container.setAttribute("aria-labelledby", "garmentConfigTitle");

  if (!garment.option) {
    const confirmation = document.createElement("p");
    confirmation.className = "garment-option-confirmation";
    confirmation.textContent = "✓ No additional options required";
    container.append(heading, confirmation);
    container.hidden = false;
    garmentOptionTimer = setTimeout(() => { container.hidden = true; }, 800);
    return;
  }

  const optionSet = optionSets[garment.option];

  const label = document.createElement("label");
  label.htmlFor = `option-${garment.id}`;
  label.textContent = `${optionSet.label} *`;

  const select = document.createElement("select");
  select.id = `option-${garment.id}`;
  select.dataset.garmentOption = garment.id;
  select.required = true;
  select.setAttribute("aria-describedby", "garment-error");
  const empty = document.createElement("option");
  empty.value = "";
  empty.textContent = "Select…";
  select.append(empty);
  optionSet.values.forEach(value => {
    const option = document.createElement("option");
    option.value = value;
    option.textContent = value;
    select.append(option);
  });
  select.value = selectedValue;
  select.addEventListener("change", () => {
    clearError("garment");
    updateWorkflowStatus();
    if (select.value) flashNextStep($("#compressionSection"));
  });
  label.append(select);
  container.append(heading, label);
  container.hidden = false;
}

function renderMeasurements(garment) {
  const content = $("#measurementContent");
  const fields = $("#measurementFields");
  const diagram = $("#measureDiagram");
  fields.replaceChildren();

  garment.measurements.forEach(key => {
    const definition = measurementDefinitions[key];
    const wrapper = document.createElement("div");
    wrapper.className = "field";
    wrapper.id = `field-${key}`;

    const label = document.createElement("label");
    label.htmlFor = key;
    label.textContent = `${definition.label} *`;
    const input = document.createElement("input");
    input.id = key;
    input.type = "number";
    input.min = "0.1";
    input.step = "0.1";
    input.inputMode = "decimal";
    input.required = true;
    input.value = measurementCache[key] || "";
    input.setAttribute("aria-describedby", `${key}-error`);
    input.addEventListener("keydown", restrictNumericKeydown);
    input.addEventListener("input", () => {
      input.value = sanitizeDecimal(input.value);
      measurementCache[key] = input.value;
      clearError(key);
      clearError("measurements");
    });

    const unit = document.createElement("span");
    unit.className = "unit";
    unit.textContent = "cm";
    const validCheck = document.createElement("span");
    validCheck.className = "measurement-valid-check";
    validCheck.textContent = "✓";
    validCheck.setAttribute("aria-hidden", "true");
    const error = document.createElement("p");
    error.id = `${key}-error`;
    error.className = "field-error";

    const updateValidState = () => wrapper.classList.toggle("is-valid", Number(input.value) > 0);
    input.addEventListener("blur", updateValidState);
    label.append(input, validCheck, unit);
    wrapper.append(label, error);
    fields.append(wrapper);
    updateValidState();
  });

  const diagramDefinition = diagrams[garment.diagram];
  diagram.innerHTML = diagramDefinition.markup;
  diagram.setAttribute("aria-label", diagramDefinition.label);
  clearTimeout(measurementTransitionTimer);
  $(".measurement-section").classList.remove("is-empty");
  content.classList.remove("is-visible");
  content.hidden = false;
  requestAnimationFrame(() => content.classList.add("is-visible"));
  $("#measurementHint").textContent = "";
}

function hideMeasurements() {
  const content = $("#measurementContent");
  clearTimeout(measurementTransitionTimer);
  $(".measurement-section").classList.add("is-empty");
  content.classList.remove("is-visible");
  if (content.hidden) {
    content.hidden = true;
  } else {
    measurementTransitionTimer = setTimeout(() => { content.hidden = true; }, MOTION_DURATION);
  }
  $("#measurementFields").replaceChildren();
  $("#measureDiagram").replaceChildren();
  $("#measurementHint").textContent = "Choose a garment style to see its measurement requirements.";
}

function readFormState() {
  cacheVisibleMeasurements();
  const garment = garmentById(currentGarmentId);
  const measurements = {};
  if (garment) {
    garment.measurements.forEach(key => {
      measurements[key] = measurementCache[key] || "";
    });
  }

  return {
    version: DRAFT_VERSION,
    savedAt: "",
    patient: {
      name: $("#name").value.trim(),
      contact: $("#contact").value.trim(),
      location: $("#location").value.trim(),
      pairs: $("#pairs").value,
      gender: document.querySelector('input[name="gender"]:checked')?.value || ""
    },
    compressionId: document.querySelector('input[name="level"]:checked')?.value || "",
    garmentId: currentGarmentId,
    garmentOption: garment?.option ? document.querySelector(`[data-garment-option="${garment.id}"]`)?.value || "" : "",
    measurements,
    notes: $("#notes").value,
    colourPriority: [...colourPriority]
  };
}

function updateWorkflowStatus() {
  const state = readFormState();
  const garment = garmentById(state.garmentId);
  const patientComplete = Boolean(
    state.patient.name &&
    state.patient.contact &&
    state.patient.location &&
    /^(?:[1-9]|1\d|20)$/.test(state.patient.pairs) &&
    state.patient.gender
  );
  const garmentComplete = Boolean(
    garment &&
    (!garment.option || optionSets[garment.option].values.includes(state.garmentOption))
  );
  const compressionComplete = Boolean(compressionLevels[state.compressionId]);
  const measurementsComplete = Boolean(
    garment && garment.measurements.every(key => Number(state.measurements[key]) > 0)
  );
  const preferencesAdded = Boolean(state.notes.trim() || state.colourPriority.length);
  const orderReady = patientComplete && garmentComplete && compressionComplete && measurementsComplete;

  if (patientComplete && !lastWorkflowCompletion.patient && !garment) flashNextStep($("#garmentSection"));
  if (measurementsComplete && !lastWorkflowCompletion.measurements) flashNextStep($("#preferencesSection"));
  lastWorkflowCompletion = { patient: patientComplete, measurements: measurementsComplete };

  setSectionStatus("patient", patientComplete ? "Complete" : "Pending", patientComplete);
  setSectionStatus("garment", garmentComplete ? "Complete" : garment ? "Options required" : "Pending", garmentComplete);
  setSectionStatus("compression", compressionComplete ? "Complete" : "Pending", compressionComplete);
  setSectionStatus("measurement", measurementsComplete ? "Complete" : garment ? "In progress" : "Waiting for style", measurementsComplete);
  setSectionStatus("preferences", preferencesAdded ? "Added" : "Optional", preferencesAdded);
  setSectionStatus("completion", orderReady ? "Ready" : "Not ready", orderReady);
  renderOrderSummary(state, garment);
}

function setSectionStatus(sectionName, text, complete) {
  const status = $(`#${sectionName}StepStatus`);
  const section = $(`#${sectionName}Section`);
  if (!status || !section) return;
  status.textContent = text;
  status.classList.toggle("is-complete", complete);
  section.classList.toggle("is-complete", complete);
}

function renderOrderSummary(state, garment) {
  const summary = $("#orderSummary");
  if (!summary) return;
  summary.replaceChildren();

  const level = compressionLevels[state.compressionId];
  const details = [
    { label: "Style", value: garment?.label || "Choose style" },
    { label: "Compression", value: level?.range || "Choose level" }
  ];
  if (garment?.option) details.push({ label: optionSets[garment.option].label, value: state.garmentOption || "Choose option" });
  details.push({ label: "Quantity", value: `QTY ${state.patient.pairs || "—"}` });

  details.forEach(detail => {
    const item = document.createElement("span");
    item.className = "summary-item";
    const label = document.createElement("small");
    label.textContent = detail.label;
    const value = document.createElement("strong");
    value.textContent = detail.value;
    item.append(label, value);
    summary.append(item);
  });
}

function validateForm() {
  const state = readFormState();
  const errors = [];
  clearAllErrors();

  if (!state.patient.name) errors.push({ key: "name", target: "name", message: "Full name is required." });
  if (!state.patient.contact) errors.push({ key: "contact", target: "contact", message: "Contact number is required." });
  if (!state.patient.location) errors.push({ key: "location", target: "location", message: "Enter a location." });
  if (!/^(?:[1-9]|1\d|20)$/.test(state.patient.pairs)) errors.push({ key: "pairs", target: "pairs", message: "Quantity must be an integer from 1 to 20." });
  if (!state.patient.gender) errors.push({ key: "gender", target: "field-gender", message: "Select a gender." });
  if (!compressionLevels[state.compressionId]) errors.push({ key: "compression", target: "compression", message: "Select a compression level." });

  const garment = garmentById(state.garmentId);
  if (!garment) {
    errors.push({ key: "garment", target: "garments", message: "Select a garment style." });
  } else {
    if (garment.option && !optionSets[garment.option].values.includes(state.garmentOption)) {
      errors.push({ key: "garment", target: `option-${garment.id}`, message: `Select ${optionSets[garment.option].label.toLowerCase()}.` });
    }
    garment.measurements.forEach(key => {
      const value = Number(state.measurements[key]);
      if (!Number.isFinite(value) || value <= 0) {
        errors.push({ key, target: key, message: `${measurementDefinitions[key].label} must be a positive number.` });
      }
    });
  }

  renderErrors(errors);
  return { valid: errors.length === 0, state, errors };
}

function renderErrors(errors) {
  const summary = $("#errorSummary");
  const list = $("#errorList");
  list.replaceChildren();

  errors.forEach(error => {
    setError(error.key, error.message);
    const li = document.createElement("li");
    const link = document.createElement("a");
    link.href = `#${error.target}`;
    link.textContent = error.message;
    link.addEventListener("click", event => {
      event.preventDefault();
      focusTarget(error.target);
    });
    li.append(link);
    list.append(li);
  });

  summary.classList.remove("is-visible");
  summary.hidden = errors.length === 0;
  if (errors.length) {
    requestAnimationFrame(() => summary.classList.add("is-visible"));
    summary.focus();
    summary.scrollIntoView({ behavior: "smooth", block: "start" });
  }
}

function setError(key, message) {
  const error = $(`#${key}-error`);
  if (error) error.textContent = message;
  const control = $(`#${key}`);
  if (control?.matches("input,select,textarea")) control.setAttribute("aria-invalid", "true");
  if (key === "gender") $("#field-gender").setAttribute("aria-invalid", "true");
}

function clearError(key) {
  const error = $(`#${key}-error`);
  if (error) error.textContent = "";
  const control = $(`#${key}`);
  control?.removeAttribute("aria-invalid");
  if (key === "gender") $("#field-gender").removeAttribute("aria-invalid");
}

function clearAllErrors() {
  document.querySelectorAll(".field-error").forEach(error => { error.textContent = ""; });
  document.querySelectorAll('[aria-invalid="true"]').forEach(element => element.removeAttribute("aria-invalid"));
  $("#errorSummary").classList.remove("is-visible");
  $("#errorSummary").hidden = true;
}

function focusTarget(id) {
  const target = $(`#${CSS.escape(id)}`);
  if (!target) return;
  const focusable = target.matches("input,select,button,textarea") ? target : target.querySelector("input,select,button,textarea") || target;
  if (!focusable.hasAttribute("tabindex") && !focusable.matches("input,select,button,textarea,a")) focusable.tabIndex = -1;
  focusable.focus();
  focusable.scrollIntoView({ behavior: "smooth", block: "center" });
}

function generatePlainTextSummary(state) {
  const level = compressionLevels[state.compressionId];
  const garment = garmentById(state.garmentId);
  const lines = [
    "COMPRESSION GARMENT ORDER",
    "",
    "PATIENT DETAILS",
    `Full Name: ${state.patient.name}`,
    `Contact Number: ${state.patient.contact}`,
    `Location: ${state.patient.location}`,
    `Quantity: ${state.patient.pairs}`,
    `Gender: ${capitalize(state.patient.gender)}`,
    "",
    "COMPRESSION",
    `Level: ${level.range}`,
    `Type: ${level.name}`,
    "",
    "GARMENT",
    `Style: ${garment.label}`
  ];

  if (state.garmentOption) lines.push(`${optionSets[garment.option].label}: ${state.garmentOption}`);
  if (state.colourPriority.length) lines.push(`Colour Priority: ${state.colourPriority.map((colour, index) => `${index + 1}. ${colour}`).join(", ")}`);
  lines.push("", "MEASUREMENTS");
  garment.measurements.forEach(key => lines.push(`${measurementDefinitions[key].label}: ${state.measurements[key]} cm`));
  if (state.notes.trim()) lines.push("", "NOTES", sanitizeText(state.notes));
  lines.push("", `Generated: ${new Date().toLocaleString()}`);
  return lines.join("\n");
}

function reviewResults() {
  const result = validateForm();
  if (!result.valid) return;
  const preview = document.createElement("textarea");
  preview.className = "preview";
  preview.readOnly = true;
  preview.value = generatePlainTextSummary(result.state);
  preview.setAttribute("aria-label", "Order text preview");

  openModal("Review Order", preview, [
    { label: "Copy Text", action: () => copyText(preview.value) },
    { label: "Print", action: printResults },
    { label: "Edit Form", action: closeModal },
    { label: "Close", action: closeModal }
  ]);
}

function buildPrintSection(title) {
  const section = document.createElement("section");
  section.className = "print-section";
  const heading = document.createElement("h2");
  heading.textContent = title;
  section.append(heading);
  return section;
}

function appendPrintRow(container, label, value, allowBlank = true) {
  const row = document.createElement("div");
  row.className = "print-row";
  const labelNode = document.createElement("span");
  labelNode.className = "print-label";
  labelNode.textContent = `${label}: `;
  row.append(labelNode);
  if (value) {
    row.append(document.createTextNode(value));
  } else if (allowBlank) {
    const blank = document.createElement("span");
    blank.className = "print-blank";
    row.append(blank);
  }
  container.append(row);
}

function preparePrintView(state) {
  const area = $("#printArea");
  area.replaceChildren();

  const header = document.createElement("header");
  header.className = "print-header";
  const brand = document.createElement("div");
  brand.className = "print-brand";
  const mark = document.createElement("span");
  mark.className = "print-mark";
  mark.textContent = "CO";
  mark.setAttribute("aria-hidden", "true");
  const brandCopy = document.createElement("div");
  const title = document.createElement("h1");
  title.textContent = "Compression Garment Order";
  const subtitle = document.createElement("p");
  subtitle.className = "print-subtitle";
  subtitle.textContent = "Clinical garment selection and measurements";
  brandCopy.append(title, subtitle);
  brand.append(mark, brandCopy);
  const generated = document.createElement("p");
  generated.className = "print-generated";
  generated.textContent = `Generated: ${new Date().toLocaleString()}`;
  header.append(brand, generated);
  area.append(header);

  const patient = buildPrintSection("Patient Details");
  const patientGrid = document.createElement("div");
  patientGrid.className = "print-grid";
  appendPrintRow(patientGrid, "Full Name", state.patient.name);
  appendPrintRow(patientGrid, "Contact Number", state.patient.contact);
  appendPrintRow(patientGrid, "Location", state.patient.location);
  appendPrintRow(patientGrid, "Quantity", state.patient.pairs);
  appendPrintRow(patientGrid, "Gender", capitalize(state.patient.gender));
  patient.append(patientGrid);
  area.append(patient);

  const compression = buildPrintSection("Compression");
  const level = compressionLevels[state.compressionId];
  appendPrintRow(compression, "Level", level ? `${level.range} — ${level.name}` : "");
  area.append(compression);

  const garmentSection = buildPrintSection("Garment");
  const garment = garmentById(state.garmentId);
  const garmentRow = document.createElement("div");
  garmentRow.className = "print-garment";
  if (garment) {
    const image = document.createElement("img");
    image.src = garment.image;
    image.alt = garment.label;
    garmentRow.append(image);
  }
  const garmentCopy = document.createElement("div");
  appendPrintRow(garmentCopy, "Style", garment?.label || "");
  if (garment?.option) appendPrintRow(garmentCopy, optionSets[garment.option].label, state.garmentOption);
  if (state.colourPriority.length) appendPrintRow(garmentCopy, "Colour Priority", state.colourPriority.map((colour, index) => `${index + 1}. ${colour}`).join(", "));
  garmentRow.append(garmentCopy);
  garmentSection.append(garmentRow);
  area.append(garmentSection);

  const measurements = buildPrintSection("Measurements");
  const measurementGrid = document.createElement("div");
  measurementGrid.className = "print-grid";
  if (garment) {
    garment.measurements.forEach(key => {
      const value = state.measurements[key] ? `${state.measurements[key]} cm` : "";
      appendPrintRow(measurementGrid, measurementDefinitions[key].label, value);
    });
  } else {
    ["Measurement 1", "Measurement 2", "Measurement 3", "Measurement 4"].forEach(label => appendPrintRow(measurementGrid, label, ""));
  }
  measurements.append(measurementGrid);
  area.append(measurements);

  const notes = buildPrintSection("Additional Notes");
  const notesBody = document.createElement("div");
  notesBody.className = "print-notes";
  notesBody.textContent = state.notes.trim() || "\n\n\n";
  notes.append(notesBody);
  area.append(notes);
}

function printResults() {
  const actionButton = $("#print");
  setActionBusy(actionButton, true);
  preparePrintView(readFormState());
  const printWindow = window.open("", "_blank");

  if (!printWindow) {
    setActionBusy(actionButton, false);
    showToast("The print preview was blocked. Allow pop-ups, then try again. Opening the browser print dialog instead.");
    window.print();
    return;
  }

  const documentRef = printWindow.document;
  documentRef.open();
  documentRef.write(`<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Compression Garment Order — Print</title><style>
    @page{size:A4;margin:14mm}*{box-sizing:border-box}body{max-width:900px;margin:0 auto;padding:22px;color:#183029;background:#fff;font:15px/1.4 Arial,sans-serif}.print-toolbar{position:sticky;top:0;display:flex;align-items:center;gap:12px;margin:-22px -22px 20px;padding:12px 22px;border-bottom:1px solid #d8e2dc;background:#f5f8f6}.print-toolbar button{min-height:44px;padding:9px 16px;border:0;border-radius:10px;background:#176b50;color:#fff;font:700 15px Arial;cursor:pointer}.print-toolbar p{margin:0;color:#42534b}.print-header{margin-bottom:16px;padding-bottom:10px;border-bottom:3px solid #176b50}.print-brand{display:flex;align-items:center;gap:10px}.print-mark{display:grid;place-items:center;width:42px;height:42px;border-radius:12px;background:#176b50;color:#fff;font-weight:800}.print-header h1{margin:0;font-size:25px}.print-subtitle{margin:2px 0 0;color:#65756e}.print-generated{margin:8px 0 0;color:#65756e;font-size:12px}.print-section{break-inside:avoid;margin:14px 0}.print-section h2{margin:0 0 7px;padding-bottom:4px;border-bottom:1px solid #aabbb1;color:#104c3a;font-size:17px}.print-grid{display:grid;grid-template-columns:repeat(2,1fr);gap:7px 18px}.print-row{min-height:22px}.print-label{font-weight:700}.print-blank{display:inline-block;min-width:150px;border-bottom:1px solid #333}.print-garment{display:flex;align-items:center;gap:12px}.print-garment img{width:62px;height:62px;object-fit:contain}.print-notes{min-height:80px;white-space:pre-wrap}@media(max-width:600px){.print-grid{grid-template-columns:1fr}.print-toolbar{align-items:flex-start;flex-direction:column}}@media print{body{max-width:none;margin:0;padding:0;font-size:10.5pt}.print-toolbar{display:none!important}}
  </style></head><body></body></html>`);
  documentRef.close();

  const toolbar = documentRef.createElement("div");
  toolbar.className = "print-toolbar";
  const printButton = documentRef.createElement("button");
  printButton.type = "button";
  printButton.textContent = "Print this page";
  printButton.addEventListener("click", () => printWindow.print());
  const instruction = documentRef.createElement("p");
  instruction.textContent = "If the print dialog does not open automatically, select this button or press Ctrl+P.";
  toolbar.append(printButton, instruction);

  const printable = $("#printArea").cloneNode(true);
  printable.removeAttribute("id");
  printable.querySelectorAll("img").forEach(image => {
    if (!image.src.startsWith("data:")) image.src = new URL(image.getAttribute("src"), window.location.href).href;
  });
  documentRef.body.append(toolbar, printable);

  showToast("Print preview opened in a new tab.");
  window.setTimeout(() => {
    printWindow.focus();
    printWindow.print();
    setActionBusy(actionButton, false);
  }, 500);
}

async function copyValidatedResults() {
  const result = validateForm();
  if (!result.valid) return;
  await copyText(generatePlainTextSummary(result.state));
}

async function copyText(value) {
  try {
    await navigator.clipboard.writeText(value);
    showToast("Order copied to the clipboard.");
  } catch {
    const preview = document.createElement("textarea");
    preview.className = "preview";
    preview.readOnly = true;
    preview.value = value;
    preview.setAttribute("aria-label", "Select and manually copy order text");
    openModal("Copy Text Manually", preview, [
      { label: "Select All", action: () => { preview.focus(); preview.select(); } },
      { label: "Close", action: closeModal }
    ]);
    showToast("Automatic copying was unavailable. Select and copy the text manually.");
  }
}

function validateWhatsAppDetails() {
  const state = readFormState();
  const errors = [];
  clearAllErrors();

  if (!/^(?:[1-9]|1\d|20)$/.test(state.patient.pairs)) errors.push({ key: "pairs", target: "pairs", message: "Quantity must be an integer from 1 to 20." });
  if (!state.patient.location) errors.push({ key: "location", target: "location", message: "Enter a location." });
  if (!compressionLevels[state.compressionId]) errors.push({ key: "compression", target: "compression", message: "Select a compression level." });

  const garment = garmentById(state.garmentId);
  if (!garment) {
    errors.push({ key: "garment", target: "garments", message: "Select a garment style." });
  } else if (garment.option && !optionSets[garment.option].values.includes(state.garmentOption)) {
    errors.push({ key: "garment", target: `option-${garment.id}`, message: `Select ${optionSets[garment.option].label.toLowerCase()}.` });
  }

  renderErrors(errors);
  return { valid: errors.length === 0, state, garment };
}

function generateWhatsAppMessage(state, garment) {
  const level = compressionLevels[state.compressionId];
  const details = [garment.description];
  if (state.garmentOption) details.push(`${optionSets[garment.option].label}: ${state.garmentOption}`);
  if (state.colourPriority.length) details.push(`Colour preference: ${state.colourPriority.map((colour, index) => `${index + 1}. ${colour}`).join(", ")}`);

  return [
    "COMPRESSION GARMENT ORDER",
    `Style: ${garment.label}`,
    `Style Details: ${details.join("; ")}`,
    `Compression Level: ${level.range} — ${level.name}`,
    `Quantity: ${state.patient.pairs}`,
    `Location: ${state.patient.location}`
  ].join("\n");
}

async function openWhatsAppMessage() {
  const result = validateWhatsAppDetails();
  if (!result.valid) return;

  const actionButton = $("#whatsapp");
  setActionBusy(actionButton, true);
  const message = generateWhatsAppMessage(result.state, result.garment);
  const whatsappWindow = window.open("about:blank", "_blank");

  try {
    try {
      await navigator.clipboard.writeText(message);
      showToast("WhatsApp message copied to the clipboard and opened.");
    } catch {
      showToast("WhatsApp opened with the message ready. Clipboard access was unavailable.");
    }

    const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(message)}`;
    if (whatsappWindow) whatsappWindow.location.replace(whatsappUrl);
    else window.location.href = whatsappUrl;
  } finally {
    setActionBusy(actionButton, false);
  }
}

function saveDraft() {
  const state = readFormState();
  state.savedAt = new Date().toISOString();
  const button = $("#save");
  try {
    localStorage.setItem(DRAFT_KEY, JSON.stringify(state));
    button.textContent = "Saved ✓";
    button.classList.add("is-saved");
    setTimeout(() => {
      button.textContent = "Save Draft";
      button.classList.remove("is-saved");
    }, 1600);
    showToast(`Draft saved on ${new Date(state.savedAt).toLocaleString()}.`);
  } catch {
    showToast("The draft could not be saved in this browser.");
  }
}

function parseDraft(raw) {
  const draft = JSON.parse(raw);
  if (!draft || draft.version !== DRAFT_VERSION || typeof draft.patient !== "object") throw new Error("Invalid draft");
  if (draft.compressionId && !compressionLevels[draft.compressionId]) throw new Error("Invalid compression");
  if (draft.garmentId && !garmentById(draft.garmentId)) throw new Error("Invalid garment");
  return draft;
}

function offerDraftRestore() {
  const raw = localStorage.getItem(DRAFT_KEY);
  if (!raw) return;
  let draft;
  try {
    draft = parseDraft(raw);
  } catch {
    localStorage.removeItem(DRAFT_KEY);
    showToast("An invalid saved draft was removed.");
    return;
  }

  const message = document.createElement("p");
  message.textContent = draft.savedAt ? `A draft saved on ${new Date(draft.savedAt).toLocaleString()} is available on this device.` : "A saved draft is available on this device.";
  openModal("Restore Saved Draft?", message, [
    { label: "Restore Draft", action: () => { applyDraft(draft); closeModal(); showToast("Draft restored."); } },
    { label: "Discard Draft", className: "danger", action: () => { localStorage.removeItem(DRAFT_KEY); closeModal(); showToast("Saved draft discarded."); } }
  ]);
}

function applyDraft(draft) {
  $("#name").value = capitalizeWords(String(draft.patient.name || "").replace(/[^\p{L}\s'-]/gu, "")).slice(0, 500);
  $("#contact").value = sanitizePhone(String(draft.patient.contact || "")).slice(0, 30);
  $("#location").value = capitalizeWords(String(draft.patient.location || "").replace(/[^\p{L}\s'.-]/gu, "")).slice(0, 200);
  $("#pairs").value = /^(?:[1-9]|1\d|20)$/.test(String(draft.patient.pairs)) ? draft.patient.pairs : "1";
  updatePairButtons();
  const gender = ["male", "female"].includes(draft.patient.gender) ? draft.patient.gender : "";
  document.querySelectorAll('input[name="gender"]').forEach(input => { input.checked = input.value === gender; });
  document.querySelectorAll('input[name="level"]').forEach(input => { input.checked = input.value === draft.compressionId; });
  updateCompressionSelection();
  $("#notes").value = capitalizeSentences(String(draft.notes || "")).slice(0, 2000);
  measurementCache = {};
  Object.entries(draft.measurements || {}).forEach(([key, value]) => {
    if (measurementDefinitions[key] && /^\d*(?:\.\d*)?$/.test(String(value))) measurementCache[key] = String(value);
  });
  colourPriority = Array.isArray(draft.colourPriority)
    ? draft.colourPriority.filter(colour => colourOptions.some(option => option.id === colour)).slice(0, colourOptions.length)
    : [];
  colourPriority = [...new Set(colourPriority)];
  updateColourChoices();

  if (draft.garmentId && garmentById(draft.garmentId)) {
    selectGarment(draft.garmentId);
    const garment = garmentById(draft.garmentId);
    if (garment.option && optionSets[garment.option].values.includes(draft.garmentOption)) {
      const select = document.querySelector(`[data-garment-option="${garment.id}"]`);
      if (select) select.value = draft.garmentOption;
    }
  }
  updateWorkflowStatus();
}

function requestClearForm() {
  const message = document.createElement("p");
  message.textContent = "All entered information and the saved draft on this device will be removed.";
  openModal("Clear Form?", message, [
    { label: "Cancel", className: "neutral", action: closeModal },
    { label: "Clear Everything", className: "danger", action: clearForm }
  ]);
}

function clearForm() {
  $("#form").reset();
  $("#pairs").value = "1";
  updatePairButtons();
  measurementCache = {};
  colourPriority = [];
  updateColourChoices();
  document.querySelectorAll(".compression-card").forEach(card => card.classList.remove("selected"));
  clearGarmentSelection({ cacheMeasurements: false });
  clearAllErrors();
  localStorage.removeItem(DRAFT_KEY);
  updateWorkflowStatus();
  closeModal();
  showToast("The form and saved draft have been cleared.");
}

function openModal(title, content, actions) {
  if (!$("#modal").hidden) closeModal(false, true);
  clearTimeout(modalTransitionTimer);
  modalOpener = document.activeElement;
  $("#modalTitle").textContent = title;
  $("#modalDescription").replaceChildren(content);
  const actionContainer = $("#modalActions");
  actionContainer.replaceChildren();

  actions.forEach(action => {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = action.label;
    if (action.className) button.className = action.className;
    button.addEventListener("click", action.action);
    actionContainer.append(button);
  });

  setBackgroundInert(true);
  $("#modal").hidden = false;
  $("#modal").classList.remove("is-open");
  modalAction = trapModalFocus;
  document.addEventListener("keydown", modalAction);
  requestAnimationFrame(() => {
    $("#modal").classList.add("is-open");
    actionContainer.querySelector("button")?.focus() || $("#modalClose").focus();
  });
}

function closeModal(restoreFocus = true, immediate = false) {
  if ($("#modal").hidden) return;
  clearTimeout(modalTransitionTimer);
  $("#modal").classList.remove("is-open");
  document.removeEventListener("keydown", modalAction);
  modalAction = null;
  if (immediate) finishModalClose(restoreFocus);
  else modalTransitionTimer = setTimeout(() => finishModalClose(restoreFocus), MOTION_DURATION);
}

function finishModalClose(restoreFocus) {
  $("#modal").hidden = true;
  setBackgroundInert(false);
  if (restoreFocus) modalOpener?.focus?.();
}

function trapModalFocus(event) {
  if (event.key === "Escape") {
    event.preventDefault();
    closeModal();
    return;
  }
  if (event.key !== "Tab") return;
  const focusable = [...$("#modal").querySelectorAll('button, textarea, input, select, a[href], [tabindex]:not([tabindex="-1"])')]
    .filter(element => !element.disabled && !element.hidden);
  if (!focusable.length) return;
  const first = focusable[0];
  const last = focusable[focusable.length - 1];
  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault();
    last.focus();
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault();
    first.focus();
  }
}

function setBackgroundInert(value) {
  document.querySelectorAll("body > *:not(#modal):not(#status)").forEach(element => {
    element.inert = value;
    if (value) element.setAttribute("aria-hidden", "true");
    else element.removeAttribute("aria-hidden");
  });
}

function showToast(message) {
  const status = $("#status");
  clearTimeout(toastTimer);
  status.textContent = message;
  status.classList.add("show");
  toastTimer = setTimeout(() => status.classList.remove("show"), 4200);
}

function flashNextStep(target) {
  if (!target || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
  if (target.classList.contains("step-nudge")) return;
  clearTimeout(stepNudgeTimers.get(target));
  target.classList.add("step-nudge");
  const timer = setTimeout(() => target.classList.remove("step-nudge"), 320);
  stepNudgeTimers.set(target, timer);
}

function setActionBusy(button, busy) {
  if (!button) return;
  button.disabled = busy;
  if (busy) button.setAttribute("aria-busy", "true");
  else button.removeAttribute("aria-busy");
}

function bindEvents() {
  $("#review").addEventListener("click", reviewResults);
  $("#print").addEventListener("click", printResults);
  $("#copy").addEventListener("click", copyValidatedResults);
  $("#whatsapp").addEventListener("click", openWhatsAppMessage);
  $("#save").addEventListener("click", saveDraft);
  $("#clear").addEventListener("click", requestClearForm);
  $("#changeGarment").addEventListener("click", () => clearGarmentSelection({ focusFirst: true }));
  $("#clearColours").addEventListener("click", clearColourPriorities);
  $("#modalClose").addEventListener("click", closeModal);
  $("#modal").addEventListener("mousedown", event => { if (event.target === $("#modal")) closeModal(); });
  $("#name").addEventListener("input", event => {
    transformInputValue(event.target, value => capitalizeWords(value.replace(/[^\p{L}\s'-]/gu, "")));
    clearError("name");
  });
  $("#contact").addEventListener("keydown", restrictPhoneKeydown);
  $("#contact").addEventListener("input", event => {
    transformInputValue(event.target, sanitizePhone);
    clearError("contact");
  });
  $("#pairs").addEventListener("keydown", restrictNumericKeydown);
  $("#pairs").addEventListener("input", event => {
    event.target.value = event.target.value.replace(/\D/g, "");
    clearError("pairs");
    updatePairButtons();
  });
  $("#pairsDecrease").addEventListener("click", () => changePairQuantity(-1));
  $("#pairsIncrease").addEventListener("click", () => changePairQuantity(1));
  $("#location").addEventListener("beforeinput", restrictLocationInput);
  $("#location").addEventListener("input", event => {
    transformInputValue(event.target, value => capitalizeWords(value.replace(/[^\p{L}\s'.-]/gu, "")));
    clearError("location");
  });
  $("#notes").addEventListener("input", event => transformInputValue(event.target, capitalizeSentences));
  document.querySelectorAll('input[name="gender"]').forEach(input => input.addEventListener("change", () => clearError("gender")));
  $("#form").addEventListener("input", updateWorkflowStatus);
  $("#form").addEventListener("change", updateWorkflowStatus);
}

function changePairQuantity(delta) {
  const input = $("#pairs");
  const current = Number.parseInt(input.value, 10);
  const base = Number.isFinite(current) ? current : 1;
  input.value = String(Math.min(20, Math.max(1, base + delta)));
  clearError("pairs");
  updatePairButtons();
  updateWorkflowStatus();
  input.focus();
}

function updatePairButtons() {
  const input = $("#pairs");
  const value = Number.parseInt(input.value, 10);
  const validValue = Number.isFinite(value) ? value : 1;
  $("#pairsDecrease").disabled = validValue <= 1;
  $("#pairsIncrease").disabled = validValue >= 20;
}

function restrictLocationInput(event) {
  if (!event.data || event.inputType.startsWith("delete") || event.inputType === "historyUndo" || event.inputType === "historyRedo") return;
  if (!/^[\p{L}\s'.-]+$/u.test(event.data)) event.preventDefault();
}

function restrictPhoneKeydown(event) {
  if (event.ctrlKey || event.metaKey || event.altKey) return;
  const allowed = ["Backspace", "Delete", "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End", "Tab", "Enter"];
  if (allowed.includes(event.key) || /^[0-9+ ]$/.test(event.key)) return;
  event.preventDefault();
}

function sanitizePhone(value) {
  const filtered = String(value).replace(/[^\d+ ]/g, "").replace(/(?!^)\+/g, "");
  return filtered.replace(/\s{2,}/g, " ");
}

function transformInputValue(input, transform) {
  const start = input.selectionStart ?? input.value.length;
  const end = input.selectionEnd ?? start;
  const transformedBefore = transform(input.value.slice(0, start));
  const transformedSelection = transform(input.value.slice(start, end));
  const transformedValue = transform(input.value);
  if (transformedValue === input.value) return;
  input.value = transformedValue;
  const nextStart = transformedBefore.length;
  input.setSelectionRange(nextStart, nextStart + transformedSelection.length);
}

function capitalizeWords(value) {
  return String(value).replace(/(^|[\s'-])(\p{L})/gu, (_, prefix, letter) => `${prefix}${letter.toLocaleUpperCase()}`);
}

function capitalizeSentences(value) {
  return String(value).replace(/(^|[.!?]\s+|\n+)(\p{L})/gu, (_, prefix, letter) => `${prefix}${letter.toLocaleUpperCase()}`);
}

function restrictNumericKeydown(event) {
  if (["e", "E", "+", "-"].includes(event.key)) event.preventDefault();
}

function sanitizeDecimal(value) {
  const cleaned = String(value).replace(/[^0-9.]/g, "");
  const [whole, ...decimals] = cleaned.split(".");
  return decimals.length ? `${whole}.${decimals.join("")}` : whole;
}

function sanitizeText(value) {
  return String(value).replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/g, "").trim();
}

function capitalize(value) {
  return value ? value.charAt(0).toUpperCase() + value.slice(1) : "";
}

initializeApp();
