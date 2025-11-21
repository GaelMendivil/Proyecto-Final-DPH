function setupPokemonAutocomplete(inputId, listId, containerClass) {
    const input = document.getElementById(inputId);
    const autocompleteList = document.getElementById(listId);
    const container = document.querySelector("." + containerClass);
    
    if (!input || !container || !autocompleteList) return;

    const searchUrl = container.dataset.url;
    let currentFocus = -1;

    input.addEventListener("input", async function() {
        let val = this.value;
        closeAllLists();
        currentFocus = -1;

        if (!val || val.length < 2) return;

        try {
            const response = await fetch(`${searchUrl}?term=${encodeURIComponent(val)}`);
            if (!response.ok) return;

            const suggestions = await response.json();

            suggestions.forEach(s => {
                const imgSource = s.imageUrl || s.ImageUrl || s.image || s.Image || s.sprite || "";
                
                const itemDiv = document.createElement("div");
                
                itemDiv.innerHTML = `
                    <img src="${imgSource}" class="autocomplete-sprite" alt="${s.name || s.Name}">
                    <span><strong>${(s.name || s.Name).substr(0, val.length)}</strong>${(s.name || s.Name).substr(val.length)}</span>
                    <input type='hidden' value='${s.name || s.Name}'> 
                `;

                itemDiv.addEventListener("click", function() {
                    input.value = this.getElementsByTagName("input")[0].value;
                    closeAllLists();
                });

                autocompleteList.appendChild(itemDiv);
            });
        } catch (error) {
            console.error("Error fetching pokemon:", error);
        }
    });

    input.addEventListener("keydown", function(e) {
        let list = autocompleteList;
        if (list) list = list.getElementsByTagName("div");
        
        if (e.keyCode == 40) { 
            currentFocus++;
            addActive(list);
        } else if (e.keyCode == 38) { 
            currentFocus--;
            addActive(list);
        } else if (e.keyCode == 13) { 
            if (currentFocus > -1) {
                if (list) {
                    e.preventDefault();
                    list[currentFocus].click();
                }
            }
        }
    });

    function addActive(x) {
        if (!x) return false;
        removeActive(x);
        if (currentFocus >= x.length) currentFocus = 0;
        if (currentFocus < 0) currentFocus = (x.length - 1);
        
        const activeItem = x[currentFocus];
        activeItem.classList.add("autocomplete-active");
        
        const listContainer = autocompleteList; 
        
        const itemTop = activeItem.offsetTop;
        const itemHeight = activeItem.offsetHeight;
        
        const containerHeight = listContainer.clientHeight;
        const containerScroll = listContainer.scrollTop;
        
        if (itemTop < containerScroll) {
            listContainer.scrollTop = itemTop;
        }
        else if (itemTop + itemHeight > containerScroll + containerHeight) {
            listContainer.scrollTop = itemTop + itemHeight - containerHeight;
        }
    }

    function removeActive(x) {
        for (let i = 0; i < x.length; i++) {
            x[i].classList.remove("autocomplete-active");
        }
    }

    function closeAllLists() {
        if (autocompleteList) autocompleteList.innerHTML = '';
        currentFocus = -1;
    }

    document.addEventListener("click", e => {
        if (e.target !== input) closeAllLists();
    });
}