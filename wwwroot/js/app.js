let currentLang = localStorage.getItem('lang') || 'ka';
let siteTexts = {};
let restaurantInfoCache = null;

function t(key) {
    return siteTexts[key] || key;
}

// Returns translated text if the key actually exists in SiteText, otherwise a design fallback
function tOr(key, fallback) {
    const v = t(key);
    return v === key ? fallback : v;
}

function exists(id) {
    return document.getElementById(id) !== null;
}

function fmtPrice(price) {
    const n = Number(price);
    return (isNaN(n) ? price : n.toFixed(2)) + '₾';
}

async function loadAndRenderAll() {
    const langMenu = document.getElementById('langMenu');
    if (langMenu) {
        langMenu.classList.remove('show');
    }

    siteTexts = await API.get('SiteText', currentLang) || {};
    restaurantInfoCache = await API.get('RestaurantInfo', currentLang);

    renderHeader();
    renderFooter();

    const path = window.location.pathname.toLowerCase();

    if (path === '/' || path === '/home' || path === '/home/index') {
        if (exists('home-hero')) renderHomeHero();
        if (exists('home-features')) await renderHomeFeatures();
    } else if (path === '/home/menu') {
        if (exists('menu-book-section')) await renderMenuBook();
    } else if (path === '/home/about') {
        if (exists('page-hero-section')) renderPageHero();
        if (exists('about-section')) renderAboutSection();
    } else if (path === '/home/location') {
        if (exists('page-hero-section')) renderPageHero();
        if (exists('location-section')) renderLocationSection();
    } else if (path === '/home/contact') {
        if (exists('page-hero-section')) renderPageHero();
        if (exists('contact-section')) renderContactSection();
    } else if (path === '/home/reservation') {
        if (exists('page-hero-section')) renderPageHero();
        if (exists('reservation-section')) await renderReservationSection();
    } else if (path === '/home/admin') {
        if (exists('admin-reservations')) await renderAdminReservations();
    }
}

// ============================================
// HEADER
// ============================================
function renderHeader() {
    const container = document.getElementById('site-header');
    const path = window.location.pathname.toLowerCase();

    const navLink = (href, label, matchPaths) => {
        const isActive = matchPaths.some(p => path === p);
        return `<a href="${href}" class="${isActive ? 'active' : ''}">${label}</a>`;
    };

    const brandName = tOr('RestaurantName', 'Bella Vista');
    const phone = restaurantInfoCache ? restaurantInfoCache.phone : '';

    container.innerHTML = `
        <header class="site-header">
            <div class="header-inner">
                <a class="logo" href="/">
                    <span class="restaurant-name">${brandName}</span>
                </a>

                <nav class="main-nav" id="mainNav">
                    ${navLink('/', t('NavHome'), ['/', '/home', '/home/index'])}
                    ${navLink('/Home/Menu', t('NavMenu'), ['/home/menu'])}
                    ${navLink('/Home/About', t('NavAbout'), ['/home/about'])}
                    ${navLink('/Home/Contact', t('NavContact'), ['/home/contact'])}
                    ${navLink('/Home/Reservation', tOr('NavReservation', 'დაჯავშნა'), ['/home/reservation'])}
                </nav>

                <div class="header-actions">
                    ${phone ? `<a class="phone-pill" href="tel:${phone}"><span>☎</span><span class="pill-text">${phone}</span></a>` : ''}

                    <div class="lang-switch">
                        <button class="lang-btn" id="langBtn">
                            <span>🌐</span> Ka • En • Ru
                        </button>
                        <div class="lang-menu" id="langMenu">
                            <button class="lang-item" data-lang="ka">Ka - ქართული</button>
                            <button class="lang-item" data-lang="en">En - English</button>
                            <button class="lang-item" data-lang="ru">Ru - Русский</button>
                        </div>
                    </div>

                    <button class="hamburger" id="hamburger">
                        <span></span>
                        <span></span>
                        <span></span>
                    </button>
                </div>
            </div>
        </header>
    `;

    // Language Menu
    const langBtn = container.querySelector('#langBtn');
    const langMenu = container.querySelector('#langMenu');

    langBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        langMenu.classList.toggle('show');
    });

    container.querySelectorAll('.lang-item').forEach(item => {
        item.addEventListener('click', (e) => {
            e.stopPropagation();
            currentLang = item.dataset.lang;
            localStorage.setItem('lang', currentLang);
            langMenu.classList.remove('show');
            loadAndRenderAll();
        });
    });

    document.addEventListener('click', (e) => {
        if (!e.target.closest('.lang-switch')) {
            langMenu.classList.remove('show');
        }
    });

    // Hamburger Menu
    const hamburger = document.getElementById('hamburger');
    const mainNav = document.getElementById('mainNav');

    if (hamburger) {
        hamburger.addEventListener('click', (e) => {
            e.stopPropagation();
            mainNav.classList.toggle('mobile-open');
            hamburger.classList.toggle('open');
        });

        mainNav.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', () => {
                mainNav.classList.remove('mobile-open');
                hamburger.classList.remove('open');
            });
        });
    }

    document.addEventListener('click', (e) => {
        if (!e.target.closest('header') && mainNav.classList.contains('mobile-open')) {
            mainNav.classList.remove('mobile-open');
            hamburger.classList.remove('open');
        }
    });
}

// ============================================
// FOOTER
// ============================================
function renderFooter() {
    const container = document.getElementById('site-footer');
    const info = restaurantInfoCache;
    if (!info) return;

    const brandName = tOr('RestaurantName', 'Bella Vista');

    container.innerHTML = `
        <footer class="site-footer">
            <div class="container footer-inner">
                <div class="footer-logo">
                    <span class="footer-name">${brandName}</span>
                </div>
                <div class="footer-info">
                    <span>📍 ${info.address}</span>
                    <span>📞 ${info.phone}</span>
                </div>
                <div class="footer-social">
                    ${info.facebookUrl ? `<a href="${info.facebookUrl}" target="_blank" rel="noopener" class="social-link" title="Facebook"><span>f</span></a>` : ''}
                    ${info.instagramUrl ? `<a href="${info.instagramUrl}" target="_blank" rel="noopener" class="social-link" title="Instagram"><span>📷</span></a>` : ''}
                </div>
            </div>
            <div class="footer-bottom">
                &copy; ${new Date().getFullYear()} ${brandName}. ${t('FooterRights')}.
            </div>
        </footer>
    `;
}

// ============================================
// PAGE HERO (generic inner pages)
// ============================================
function renderPageHero() {
    const container = document.getElementById('page-hero-section');
    const titleKey = container.dataset.titleKey;

    container.innerHTML = `
        <div class="page-hero">
            <div class="container"><h1>${t(titleKey)}</h1></div>
        </div>
    `;
}

// ============================================
// HOME — HERO
// ============================================
function renderHomeHero() {
    const container = document.getElementById('home-hero');
    if (!container) return;

    const brandName = tOr('RestaurantName', 'Bella Vista');
    const heroTitle = tOr('HeroTitle', brandName);

    container.innerHTML = `
        <section class="home-hero">
            <div class="home-hero-inner">
                <span class="hero-eyebrow">${tOr('HeroEyebrow', 'Welcome to')}</span>
                <h1 class="hero-title">${heroTitle}</h1>
                <div class="hero-divider"></div>
                <p class="hero-subtitle">${tOr('HeroSubtitle', 'Delicious food, fine wines and great ambiance.')}</p>
                <a href="/Home/Menu" class="btn btn-gold">${tOr('HeroCta', 'View Menu')}</a>
            </div>
        </section>
    `;
}

// ============================================
// HOME — FEATURE STRIP (Menu / About / Location)
// ============================================
async function renderHomeFeatures() {
    const container = document.getElementById('home-features');
    if (!container) return;

    const items = await API.get('MenuItem', currentLang);
    const info = restaurantInfoCache;
    const featuredImg = (items || []).find(i => i.imageUrl)?.imageUrl;

    const locationMedia = info && info.mapEmbedUrl
        ? `<div class="feature-card-media map-media"><iframe src="${info.mapEmbedUrl}" loading="lazy" title="map"></iframe></div>`
        : `<div class="feature-card-media"><img src="/images/about.jpg" alt="Location" /></div>`;

    container.innerHTML = `
        <section class="feature-strip">
            <div class="container feature-grid">
                <div class="feature-card">
                    <div class="feature-card-media">
                        <img src="${featuredImg || '/images/about.jpg'}" alt="${tOr('FeatureMenuTitle', 'Our Menu')}" />
                    </div>
                    <h3>${tOr('FeatureMenuTitle', 'Our Menu')}</h3>
                    <div class="feature-underline"></div>
                    <p>${tOr('FeatureMenuText', "A selection of our chef's special dishes.")}</p>
                    <a href="/Home/Menu" class="btn btn-outline-dark">${tOr('FeatureMenuBtn', 'See Full Menu')}</a>
                </div>

                <div class="feature-card">
                    <div class="feature-card-media">
                        <img src="/images/about.jpg" alt="${tOr('FeatureAboutTitle', 'About Us')}" />
                    </div>
                    <h3>${tOr('FeatureAboutTitle', 'About Us')}</h3>
                    <div class="feature-underline"></div>
                    <p>${tOr('FeatureAboutText', 'We offer a unique dining experience with the finest ingredients.')}</p>
                    <a href="/Home/About" class="btn btn-outline-dark">${tOr('FeatureAboutBtn', 'Learn More')}</a>
                </div>

                <div class="feature-card">
                    ${locationMedia}
                    <h3>${tOr('FeatureLocationTitle', 'Location')}</h3>
                    <div class="feature-underline"></div>
                    <p>${info ? info.address : ''}${info && info.phone ? '<br/>' + info.phone : ''}</p>
                    <a href="/Home/Location" class="btn btn-outline-dark">${tOr('FeatureLocationBtn', 'Get Directions')}</a>
                </div>
            </div>
        </section>
    `;
}

// ============================================
// MENU — BOOK
// ============================================
function buildMenuColumns(categories, items, perColumn) {
    const columns = [];
    let col = [];
    let count = 0;

    categories.forEach(cat => {
        const catItems = items.filter(i => i.categoryId === cat.id);
        if (catItems.length === 0) return;

        if (count >= perColumn) {
            columns.push(col);
            col = [];
            count = 0;
        }
        col.push({ type: 'header', text: cat.name });

        catItems.forEach(item => {
            if (count >= perColumn) {
                columns.push(col);
                col = [];
                count = 0;
                col.push({ type: 'header', text: cat.name });
            }
            col.push({ type: 'item', item });
            count++;
        });
    });

    if (col.length) columns.push(col);
    return columns;
}

function renderMenuColumnHtml(col, opts) {
    opts = opts || {};
    if (!col) {
        return `<div class="book-page-empty">${tOr('MenuMoreSoon', 'More coming soon')}</div>`;
    }

    let html = '';
    if (opts.showTitle) {
        html += `<div class="book-page-title">${tOr('MenuBookTitle', 'Our Menu')}</div>`;
    }

    col.forEach(entry => {
        if (entry.type === 'header') {
            html += `<div class="menu-category-label">${entry.text}</div>`;
        } else {
            const item = entry.item;
            html += `
                <div class="menu-row">
                    ${item.imageUrl ? `<div class="menu-row-thumb"><img src="${item.imageUrl}" alt="${item.name}" loading="lazy" /></div>` : ''}
                    <div class="menu-row-body">
                        <div class="menu-row-top">
                            <span class="menu-row-name">${item.name}</span>
                            <span class="menu-row-price">${fmtPrice(item.price)}</span>
                        </div>
                        ${item.description ? `<p class="menu-row-desc">${item.description}</p>` : ''}
                    </div>
                </div>
            `;
        }
    });

    if (opts.showThanks) {
        html += `<div class="book-thanks">${tOr('MenuThanks', 'Thank you for dining with us!')}</div>`;
    }

    return html;
}

async function renderMenuBook() {
    const container = document.getElementById('menu-book-section');
    if (!container) return;

    const [categories, items] = await Promise.all([
        API.get('MenuCategory', currentLang),
        API.get('MenuItem', currentLang)
    ]);

    if (!categories || !items) return;

    container.innerHTML = `
        <div class="menu-stage" id="menuStageWrap"></div>
    `;

    const columns = buildMenuColumns(categories, items, 4);
    const spreads = [];
    for (let i = 0; i < columns.length; i += 2) {
        spreads.push([columns[i], columns[i + 1] || null]);
    }
    if (spreads.length === 0) spreads.push([[], null]);

    let currentSpread = 0;
    const stageWrap = document.getElementById('menuStageWrap');

    function renderSpread() {
        const [left, right] = spreads[currentSpread];
        const isFirst = currentSpread === 0;
        const isLast = currentSpread === spreads.length - 1;

        stageWrap.innerHTML = `
            <div class="menu-stage-inner">
                <button class="stage-nav" id="menuPrev" ${isFirst ? 'disabled' : ''}>
                    <span class="arrow-circle">‹</span><span class="label">${tOr('LabelPrev', 'Prev')}</span>
                </button>

                <div class="book">
                    <a href="/" class="book-close" aria-label="Close menu">×</a>
                    <div class="book-pages">
                        <div class="book-page">${renderMenuColumnHtml(left, { showTitle: isFirst })}</div>
                        <div class="book-page">${renderMenuColumnHtml(right, { showThanks: isLast && !!right })}</div>
                    </div>
                </div>

                <button class="stage-nav" id="menuNext" ${isLast ? 'disabled' : ''}>
                    <span class="arrow-circle">›</span><span class="label">${tOr('LabelNext', 'Next')}</span>
                </button>
            </div>
        `;

        document.getElementById('menuPrev')?.addEventListener('click', () => {
            if (currentSpread > 0) { currentSpread--; renderSpread(); }
        });
        document.getElementById('menuNext')?.addEventListener('click', () => {
            if (currentSpread < spreads.length - 1) { currentSpread++; renderSpread(); }
        });
    }

    renderSpread();
}

// ============================================
// ABOUT
// ============================================
function renderAboutSection() {
    const container = document.getElementById('about-section');
    if (!container) return;
    container.innerHTML = `
        <div class="container section">
            <p class="about-text">${t('AboutText')}</p>
        </div>
    `;
}

// ============================================
// LOCATION
// ============================================
function renderLocationSection() {
    const container = document.getElementById('location-section');
    if (!container) return;

    const info = restaurantInfoCache;
    if (!info) return;

    container.innerHTML = `
        <div class="container section">
            <div class="info-panel">
                <ul class="contact-list">
                    <li><span class="label">${t('LabelAddress')}</span><span>${info.address}</span></li>
                    <li><span class="label">${t('LabelPhone')}</span><span>${info.phone}</span></li>
                </ul>
                ${info.mapEmbedUrl ? `<div class="map-frame"><iframe src="${info.mapEmbedUrl}" loading="lazy" title="map"></iframe></div>` : ''}
            </div>
        </div>
    `;
}

// ============================================
// CONTACT
// ============================================
function renderContactSection() {
    const container = document.getElementById('contact-section');
    if (!container) return;

    const info = restaurantInfoCache;
    if (!info) return;

    container.innerHTML = `
        <div class="container section">
            <div class="info-panel">
                <p class="about-text" style="margin-bottom:28px;">${t('ContactText')}</p>
                <ul class="contact-list">
                    <li><span class="label">${t('LabelAddress')}</span><span>${info.address}</span></li>
                    <li><span class="label">${t('LabelPhone')}</span><span>${info.phone}</span></li>
                    <li><span class="label">${t('LabelHours')}</span><span>${info.workingHours}</span></li>
                    <li><span class="label">${t('LabelEmail')}</span><span>${info.email}</span></li>
                </ul>
            </div>
        </div>
    `;
}

// ============================================
// RESERVATION
// ============================================
let reservationCart = {};
let selectedTableId = null;
let selectedTableInfo = null;

async function renderReservationSection() {
    const container = document.getElementById('reservation-section');
    if (!container) return;

    reservationCart = {};
    selectedTableId = null;
    selectedTableInfo = null;

    container.innerHTML = `
        <div class="container section">
            <!-- STEP 1: თარიღი, დრო, სტუმრები -->
            <div class="res-step" id="resStep1">
                <h3 class="res-menu-title">${tOr('LabelStep1', 'აირჩიეთ თარიღი, დრო და სტუმრების რაოდენობა')}</h3>
                <div class="res-form-grid">
                    <div class="res-field">
                        <label>${tOr('LabelDate', 'თარიღი')}</label>
                        <input type="date" id="resDate" required />
                        <span class="field-error" id="resDateError"></span>
                    </div>
                    <div class="res-field">
                        <label>${tOr('LabelTime', 'დრო')}</label>
                        <input type="time" id="resTime" required />
                    </div>
                    <div class="res-field">
                        <label>${tOr('LabelGuests', 'სტუმრების რაოდენობა')}</label>
                        <input type="number" id="resGuests" min="1" value="2" required />
                    </div>
                </div>
                <button type="button" class="btn btn-gold" id="findTablesBtn" style="margin-top:16px;">${tOr('BtnFindTables', 'თავისუფალი მაგიდების ძიება')}</button>
            </div>

            <!-- STEP 2: მაგიდის არჩევა -->
            <div class="res-step" id="resStep2" style="display:none;">
                <h3 class="res-menu-title">${tOr('LabelStep2', 'აირჩიეთ მაგიდა')}</h3>
                <div id="tablesGrid" class="tables-select-grid"></div>
                <button type="button" class="pay-btn cancel-btn" id="backToStep1Btn" style="margin-top:16px; max-width:200px;">← უკან</button>
            </div>

            <!-- STEP 3: ფორმა + მენიუ -->
            <div class="res-step" id="resStep3" style="display:none;">
                <div class="res-selected-table" id="selectedTableBanner"></div>

                <form id="reservationForm" class="reservation-form">
                    <div class="res-form-grid">
                        <div class="res-field">
                            <label>${tOr('LabelName', 'სახელი')}</label>
                            <input type="text" id="resName" required minlength="2" maxlength="10" placeholder="მხოლოდ ასოები" />
                            <span class="field-error" id="resNameError"></span>
                        </div>
                        <div class="res-field">
                            <label>${tOr('LabelPhoneField', 'ტელეფონი')}</label>
                            <input type="tel" id="resPhone" required maxlength="9" inputmode="numeric" placeholder="9 ციფრი" />
                            <span class="field-error" id="resPhoneError"></span>
                        </div>
                    </div>

                    <h3 class="res-menu-title">${tOr('LabelChooseFood', 'აირჩიეთ კერძები')}</h3>
                    <div class="res-menu-list" id="resMenuList"></div>

                    <div class="res-summary" id="resSummary">
                        <div class="res-summary-row"><span>${tOr('LabelTotal', 'სრული თანხა')}</span><span id="resTotal">0.00₾</span></div>
                        <div class="res-summary-row"><span>${tOr('LabelPrepaid', 'წინასწარ გადასახდელი (50%)')}</span><span id="resPrepaid">0.00₾</span></div>
                        <div class="res-summary-row"><span>${tOr('LabelRemaining', 'დარჩენილი (რესტორანში)')}</span><span id="resRemaining">0.00₾</span></div>
                    </div>

                    <div id="resMessage" class="res-message"></div>

                    <button type="submit" class="btn btn-gold res-submit-btn">${tOr('BtnSubmitReservation', 'დაჯავშნა')}</button>
                </form>
            </div>
        </div>
    `;

    initStep1();
}

function initStep1() {
    const dateInput = document.getElementById('resDate');
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    dateInput.setAttribute('min', tomorrow.toISOString().split('T')[0]);

    document.getElementById('findTablesBtn').addEventListener('click', findAvailableTables);
}

async function findAvailableTables() {
    const dateInput = document.getElementById('resDate');
    const timeInput = document.getElementById('resTime');
    const guestsInput = document.getElementById('resGuests');
    const dateError = document.getElementById('resDateError');

    dateError.textContent = '';
    dateInput.classList.remove('field-invalid');

    const dateVal = dateInput.value;
    const timeVal = timeInput.value;
    const guestsVal = parseInt(guestsInput.value);

    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(0, 0, 0, 0);

    if (!dateVal || new Date(dateVal) < tomorrow) {
        dateError.textContent = 'დაჯავშნა შესაძლებელია მხოლოდ ხვალინდელი დღიდან';
        dateInput.classList.add('field-invalid');
        return;
    }
    if (!timeVal || !guestsVal || guestsVal < 1) {
        return;
    }

    const btn = document.getElementById('findTablesBtn');
    btn.disabled = true;
    btn.textContent = 'იძებნება...';

    try {
        const res = await fetch(`/api/Reservation/AvailableTables?date=${dateVal}&time=${timeVal}&guests=${guestsVal}`);
        const tables = await res.json();

        const grid = document.getElementById('tablesGrid');

        if (!tables || tables.length === 0) {
            grid.innerHTML = `<div class="empty-msg">🔴 არჩეულ დროს თავისუფალი მაგიდა არ არის.</div>`;
        } else {
            grid.innerHTML = tables.map(t => `
                <div class="table-select-card" data-table-id="${t.id}" data-table-number="${t.tableNumber}" data-table-capacity="${t.capacity}">
                    <div class="ts-icon">🪑</div>
                    <div class="ts-name">მაგიდა №${t.tableNumber}</div>
                    <div class="ts-cap">${t.capacity} ადგილი</div>
                    <div class="ts-status">🟢 თავისუფალია</div>
                    <button type="button" class="btn-small btn-assign ts-choose-btn">არჩევა</button>
                </div>
            `).join('');

            grid.querySelectorAll('.ts-choose-btn').forEach(b => {
                b.addEventListener('click', (e) => {
                    const card = e.target.closest('.table-select-card');
                    selectedTableId = parseInt(card.dataset.tableId);
                    selectedTableInfo = { number: card.dataset.tableNumber, capacity: card.dataset.tableCapacity };
                    goToStep3();
                });
            });
        }

        document.getElementById('resStep1').style.display = 'none';
        document.getElementById('resStep2').style.display = 'block';

        document.getElementById('backToStep1Btn').addEventListener('click', () => {
            document.getElementById('resStep2').style.display = 'none';
            document.getElementById('resStep1').style.display = 'block';
        });

    } catch (err) {
        console.error(err);
    }

    btn.disabled = false;
    btn.textContent = tOr('BtnFindTables', 'თავისუფალი მაგიდების ძიება');
}

async function goToStep3() {
    document.getElementById('resStep2').style.display = 'none';
    document.getElementById('resStep3').style.display = 'block';

    document.getElementById('selectedTableBanner').innerHTML =
        `🪑 არჩეული მაგიდა: <strong>№${selectedTableInfo.number}</strong> (${selectedTableInfo.capacity} ადგილი) — ${document.getElementById('resDate').value}, ${document.getElementById('resTime').value}`;

    const [categories, items] = await Promise.all([
        API.get('MenuCategory', currentLang),
        API.get('MenuItem', currentLang)
    ]);

    let menuHtml = '';
    categories.forEach(cat => {
        const catItems = items.filter(i => i.categoryId === cat.id);
        if (catItems.length === 0) return;

        const isKhinkali = cat.name.includes('ხინკალი') || cat.name.toLowerCase().includes('khinkali');
        const step = isKhinkali ? 5 : 1;

        const itemsHtml = catItems.map(item => `
            <div class="res-item-row" data-item-id="${item.id}" data-price="${item.price}" data-step="${step}">
                <div class="res-item-info">
                    <span class="res-item-name">${item.name}</span>
                    <span class="res-item-price">${fmtPrice(item.price)}</span>
                </div>
                <div class="res-qty-control">
                    <button type="button" class="qty-btn qty-minus" data-item-id="${item.id}">−</button>
                    <span class="qty-value" id="qty-${item.id}">0</span>
                    <button type="button" class="qty-btn qty-plus" data-item-id="${item.id}">+</button>
                </div>
            </div>
        `).join('');

        menuHtml += `<div class="res-category-block"><h4 class="res-category-title">${cat.name}</h4>${itemsHtml}</div>`;
    });

    document.getElementById('resMenuList').innerHTML = menuHtml;
    initReservationForm();
}

function initReservationForm() {
    const container = document.getElementById('reservation-section');

    container.querySelectorAll('.qty-plus').forEach(btn => {
        btn.addEventListener('click', () => {
            const id = btn.dataset.itemId;
            const row = container.querySelector(`.res-item-row[data-item-id="${id}"]`);
            const step = parseInt(row?.dataset.step || '1');
            reservationCart[id] = (reservationCart[id] || 0) + step;
            updateReservationUI(id);
        });
    });

    container.querySelectorAll('.qty-minus').forEach(btn => {
        btn.addEventListener('click', () => {
            const id = btn.dataset.itemId;
            const row = container.querySelector(`.res-item-row[data-item-id="${id}"]`);
            const step = parseInt(row?.dataset.step || '1');
            const current = reservationCart[id] || 0;
            if (current >= step) {
                reservationCart[id] = current - step;
                updateReservationUI(id);
            }
        });
    });

    const nameInput = document.getElementById('resName');
    if (nameInput) {
        nameInput.addEventListener('input', () => {
            nameInput.value = nameInput.value.replace(/[^a-zA-Zა-ჰ]/g, '');
        });
    }

    const phoneInput = document.getElementById('resPhone');
    if (phoneInput) {
        phoneInput.addEventListener('input', () => {
            phoneInput.value = phoneInput.value.replace(/\D/g, '').slice(0, 9);
        });
    }

    document.getElementById('reservationForm').addEventListener('submit', submitReservation);
}

function updateReservationUI(itemId) {
    const qty = reservationCart[itemId] || 0;
    const qtyEl = document.getElementById(`qty-${itemId}`);
    if (qtyEl) qtyEl.textContent = qty;
    recalcReservationTotals();
}

function recalcReservationTotals() {
    const container = document.getElementById('reservation-section');
    let total = 0;

    container.querySelectorAll('.res-item-row').forEach(row => {
        const id = row.dataset.itemId;
        const price = parseFloat(row.dataset.price);
        const qty = reservationCart[id] || 0;
        total += price * qty;
    });

    const prepaid = total * 0.5;
    const remaining = total - prepaid;

    document.getElementById('resTotal').textContent = fmtPrice(total.toFixed(2));
    document.getElementById('resPrepaid').textContent = fmtPrice(prepaid.toFixed(2));
    document.getElementById('resRemaining').textContent = fmtPrice(remaining.toFixed(2));
}

async function submitReservation(e) {
    e.preventDefault();

    const messageEl = document.getElementById('resMessage');
    messageEl.textContent = '';
    messageEl.className = 'res-message';

    const nameInput = document.getElementById('resName');
    const phoneInput = document.getElementById('resPhone');

    [nameInput, phoneInput].forEach(el => el.classList.remove('field-invalid'));
    document.getElementById('resNameError').textContent = '';
    document.getElementById('resPhoneError').textContent = '';

    const nameVal = nameInput.value.trim();
    const phoneVal = phoneInput.value.trim();

    let hasError = false;

    if (nameVal.length < 2 || nameVal.length > 10 || !/^[a-zA-Zა-ჰ]+$/.test(nameVal)) {
        document.getElementById('resNameError').textContent = 'სახელი უნდა იყოს 2-10 ასო, მხოლოდ ასოებით';
        nameInput.classList.add('field-invalid');
        hasError = true;
    }

    if (!/^\d{9}$/.test(phoneVal)) {
        document.getElementById('resPhoneError').textContent = 'ტელეფონი უნდა შეიცავდეს ზუსტად 9 ციფრს';
        phoneInput.classList.add('field-invalid');
        hasError = true;
    }

    if (hasError) return;

    const items = Object.entries(reservationCart)
        .filter(([id, qty]) => qty > 0)
        .map(([id, qty]) => ({ menuItemId: parseInt(id), quantity: qty }));

    if (items.length === 0) {
        messageEl.textContent = tOr('ErrorNoItems', 'გთხოვთ აირჩიოთ მინიმუმ ერთი კერძი');
        messageEl.className = 'res-message error';
        return;
    }

    const payload = {
        customerName: nameVal,
        phone: phoneVal,
        reservationDate: document.getElementById('resDate').value,
        reservationTime: document.getElementById('resTime').value,
        guestCount: parseInt(document.getElementById('resGuests').value),
        tableId: selectedTableId,
        items: items
    };

    const submitBtn = document.querySelector('.res-submit-btn');
    submitBtn.disabled = true;
    submitBtn.textContent = tOr('BtnSubmitting', 'იგზავნება...');

    try {
        const response = await fetch('/api/Reservation', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const data = await response.json();

        if (!response.ok) {
            // 409 Conflict — მაგიდა უკვე დაიკავეს
            messageEl.textContent = data.error || tOr('ErrorGeneric', 'დაფიქსირდა შეცდომა');
            messageEl.className = 'res-message error';
            submitBtn.disabled = false;
            submitBtn.textContent = tOr('BtnSubmitReservation', 'დაჯავშნა');
            return;
        }

        messageEl.textContent = tOr('ReservationSuccess', 'თქვენი ჯავშანი წარმატებით შესრულდა!');
        messageEl.className = 'res-message success';
        document.getElementById('reservationForm').reset();
        reservationCart = {};
        document.querySelectorAll('.qty-value').forEach(el => el.textContent = '0');
        recalcReservationTotals();
        submitBtn.disabled = false;
        submitBtn.textContent = tOr('BtnSubmitReservation', 'დაჯავშნა');

    } catch (err) {
        messageEl.textContent = tOr('ErrorGeneric', 'დაფიქსირდა შეცდომა, სცადეთ ხელახლა');
        messageEl.className = 'res-message error';
        submitBtn.disabled = false;
        submitBtn.textContent = tOr('BtnSubmitReservation', 'დაჯავშნა');
    }
}

// ============================================
// ADMIN — RESERVATIONS LIST
// ============================================
async function renderAdminReservations() {
    const container = document.getElementById('admin-reservations');
    if (!container) return;

    container.innerHTML = '<p>იტვირთება...</p>';

    try {
        const response = await fetch('/api/Reservation');
        const reservations = await response.json();

        if (!reservations || reservations.length === 0) {
            container.innerHTML = '<p>ჯერ არცერთი ჯავშანი არ არის.</p>';
            return;
        }

        const rowsHtml = reservations.map(r => {
            const itemsHtml = r.items.map(i =>
                `<div class="admin-item-line">${i.menuItemName} × ${i.quantity} = ${i.lineTotal.toFixed(2)}₾</div>`
            ).join('');

            return `
                <div class="admin-card">
                    <div class="admin-card-header">
                        <div>
                            <strong>${r.customerName}</strong> — ${r.phone}
                        </div>
                        <div class="admin-status">
                            <span class="badge status-${r.reservationStatus.toLowerCase()}">${r.reservationStatus}</span>
                            <span class="badge payment-${r.paymentStatus.toLowerCase()}">${r.paymentStatus}</span>
                        </div>
                    </div>
                    <div class="admin-card-body">
                        <div class="admin-info-row">
                            <span>📅 ${r.reservationDate}</span>
                            <span>🕐 ${r.reservationTime}</span>
                            <span>👥 ${r.guestCount} სტუმარი</span>
                        </div>
                        <div class="admin-items-list">
                            ${itemsHtml}
                        </div>
                        <div class="admin-totals">
                            <span>სრული: <strong>${r.totalAmount.toFixed(2)}₾</strong></span>
                            <span>წინასწარ: <strong>${r.prepaidAmount.toFixed(2)}₾</strong></span>
                            <span>დარჩენილი: <strong>${r.remainingAmount.toFixed(2)}₾</strong></span>
                        </div>
                        <div class="admin-created">შეკვეთის დრო: ${r.createdAt}</div>
                    </div>
                </div>
            `;
        }).join('');

        container.innerHTML = `<div class="admin-cards-wrap">${rowsHtml}</div>`;

    } catch (err) {
        container.innerHTML = '<p>შეცდომა მონაცემების ჩატვირთვისას.</p>';
    }
}

document.addEventListener('DOMContentLoaded', loadAndRenderAll);