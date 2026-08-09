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

document.addEventListener('DOMContentLoaded', loadAndRenderAll);