// Car Lookup — cascading make/year lookup against the app's own /api/vehicles endpoints.
(function () {
    'use strict';

    const SEARCH_DEBOUNCE_MS = 200;

    const form = document.getElementById('lookup-form');
    const makeInput = document.getElementById('make-input');
    const makeOptions = document.getElementById('make-options');
    const yearSelect = document.getElementById('year-select');
    const typeSelect = document.getElementById('type-select');
    const searchButton = document.getElementById('search-button');
    const resetButton = document.getElementById('reset-button');
    const status = document.getElementById('status');
    const results = document.getElementById('results');
    const typesList = document.getElementById('types-list');
    const modelsList = document.getElementById('models-list');
    const typesCaption = document.getElementById('types-caption');
    const modelsCaption = document.getElementById('models-caption');

    /** The make the user actually picked from the list, not just what they typed. */
    let selectedMake = null;
    /** Index of the highlighted suggestion, or -1 when nothing is highlighted. */
    let highlightedIndex = -1;
    let suggestions = [];
    let suggestRequest = 0;
    let debounceTimer = null;

    async function getJson(url) {
        const response = await fetch(url, { headers: { Accept: 'application/json' } });

        if (!response.ok) {
            let message = 'Something went wrong while loading vehicle data.';

            try {
                const problem = await response.json();
                if (problem && problem.detail) {
                    message = problem.detail;
                }
            } catch {
                // Response body was not a problem document; keep the generic message.
            }

            throw new Error(message);
        }

        return response.json();
    }

    function setStatus(message, kind) {
        status.textContent = message || '';
        status.className = message ? 'status status-' + (kind || 'info') : 'status';
    }

    function closeSuggestions() {
        makeOptions.hidden = true;
        makeOptions.replaceChildren();
        makeInput.setAttribute('aria-expanded', 'false');
        highlightedIndex = -1;
        suggestions = [];
    }

    function highlight(index) {
        const items = Array.from(makeOptions.children);
        if (items.length === 0) {
            return;
        }

        highlightedIndex = (index + items.length) % items.length;

        items.forEach((item, i) => {
            const isActive = i === highlightedIndex;
            item.classList.toggle('is-active', isActive);
            item.setAttribute('aria-selected', String(isActive));
        });

        items[highlightedIndex].scrollIntoView({ block: 'nearest' });
    }

    function renderSuggestions(makes) {
        suggestions = makes;

        if (makes.length === 0) {
            makeOptions.replaceChildren();
            makeOptions.hidden = true;
            makeInput.setAttribute('aria-expanded', 'false');
            return;
        }

        makeOptions.replaceChildren(...makes.map((make, index) => {
            const item = document.createElement('li');
            item.className = 'combobox-option';
            item.id = 'make-option-' + index;
            item.setAttribute('role', 'option');
            item.setAttribute('aria-selected', 'false');
            item.textContent = make.makeName;
            // mousedown fires before the input's blur, so the click is not lost.
            item.addEventListener('mousedown', event => {
                event.preventDefault();
                selectMake(make);
            });
            return item;
        }));

        makeOptions.hidden = false;
        makeInput.setAttribute('aria-expanded', 'true');
        highlightedIndex = -1;
    }

    async function suggest(query) {
        const requestId = ++suggestRequest;

        try {
            const makes = await getJson('/api/vehicles/makes?limit=25&query=' + encodeURIComponent(query));

            // Ignore responses that arrived out of order behind a newer keystroke.
            if (requestId === suggestRequest) {
                renderSuggestions(makes);
            }
        } catch (error) {
            if (requestId === suggestRequest) {
                closeSuggestions();
                setStatus(error.message, 'error');
            }
        }
    }

    async function selectMake(make) {
        selectedMake = make;
        makeInput.value = make.makeName;
        closeSuggestions();
        searchButton.disabled = false;
        setStatus('');

        await loadVehicleTypes(make);
    }

    async function loadVehicleTypes(make) {
        typeSelect.disabled = true;
        typeSelect.replaceChildren(new Option('Loading types…', ''));

        try {
            const types = await getJson('/api/vehicles/makes/' + make.makeId + '/vehicle-types');

            typeSelect.replaceChildren(
                new Option('All types', ''),
                ...types.map(type => new Option(type.vehicleTypeName, type.vehicleTypeName)));
            typeSelect.disabled = types.length === 0;

            return types;
        } catch (error) {
            typeSelect.replaceChildren(new Option('All types', ''));
            typeSelect.disabled = true;
            setStatus(error.message, 'error');

            return null;
        }
    }

    function renderTypes(make, types) {
        typesCaption.textContent = types.length === 0
            ? 'vPIC lists no vehicle types for ' + make.makeName + '.'
            : make.makeName + ' builds ' + types.length + ' vehicle ' + (types.length === 1 ? 'type' : 'types') + '.';

        typesList.replaceChildren(...types.map(type => {
            const chip = document.createElement('span');
            chip.className = 'chip';
            chip.textContent = type.vehicleTypeName;
            return chip;
        }));
    }

    function renderModels(make, year, vehicleType, models) {
        const scope = vehicleType ? vehicleType + ' models' : 'models';

        modelsCaption.textContent = models.length === 0
            ? 'No ' + scope + ' found for ' + make.makeName + ' in ' + year + '.'
            : models.length + ' ' + scope + ' for ' + make.makeName + ' in ' + year + '.';

        modelsList.replaceChildren(...models.map(model => {
            const card = document.createElement('div');
            card.className = 'model-card';

            const name = document.createElement('span');
            name.className = 'model-name';
            name.textContent = model.modelName;
            card.appendChild(name);

            if (model.vehicleTypeName) {
                const type = document.createElement('span');
                type.className = 'model-type';
                type.textContent = model.vehicleTypeName;
                card.appendChild(type);
            }

            return card;
        }));
    }

    async function search() {
        if (!selectedMake) {
            setStatus('Pick a make from the list first.', 'error');
            return;
        }

        const make = selectedMake;
        const year = yearSelect.value;
        const vehicleType = typeSelect.value;

        searchButton.disabled = true;
        setStatus('Looking up ' + make.makeName + ' ' + year + '…', 'busy');

        try {
            const modelsUrl = '/api/vehicles/makes/' + make.makeId + '/models?year=' + encodeURIComponent(year) +
                (vehicleType ? '&vehicleType=' + encodeURIComponent(vehicleType) : '');

            const [types, models] = await Promise.all([
                getJson('/api/vehicles/makes/' + make.makeId + '/vehicle-types'),
                getJson(modelsUrl)
            ]);

            renderTypes(make, types);
            renderModels(make, year, vehicleType, models);

            results.hidden = false;
            setStatus('');
        } catch (error) {
            results.hidden = true;
            setStatus(error.message, 'error');
        } finally {
            searchButton.disabled = false;
        }
    }

    makeInput.addEventListener('input', () => {
        // Typing invalidates any previous pick: the id must come from the list, not the text box.
        selectedMake = null;
        searchButton.disabled = true;
        typeSelect.disabled = true;
        typeSelect.replaceChildren(new Option('All types', ''));

        const query = makeInput.value.trim();
        window.clearTimeout(debounceTimer);

        if (query.length === 0) {
            closeSuggestions();
            return;
        }

        debounceTimer = window.setTimeout(() => suggest(query), SEARCH_DEBOUNCE_MS);
    });

    makeInput.addEventListener('keydown', event => {
        if (makeOptions.hidden) {
            return;
        }

        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                highlight(highlightedIndex + 1);
                break;
            case 'ArrowUp':
                event.preventDefault();
                highlight(highlightedIndex - 1);
                break;
            case 'Enter':
                if (highlightedIndex >= 0) {
                    event.preventDefault();
                    selectMake(suggestions[highlightedIndex]);
                }
                break;
            case 'Escape':
                closeSuggestions();
                break;
        }
    });

    makeInput.addEventListener('blur', () => window.setTimeout(closeSuggestions, 100));

    form.addEventListener('submit', event => {
        event.preventDefault();
        search();
    });

    resetButton.addEventListener('click', () => {
        selectedMake = null;
        makeInput.value = '';
        typeSelect.replaceChildren(new Option('All types', ''));
        typeSelect.disabled = true;
        searchButton.disabled = true;
        results.hidden = true;
        closeSuggestions();
        setStatus('');
        makeInput.focus();
    });
})();
