#!/usr/bin/env node

/**
 * Art Nouveau Visual Anchoring MCP Server
 * Provides reference images of Alphonse Mucha's work and Art Nouveau architecture
 * for AI agents to use the "anchoring" technique in visual generation tasks.
 */

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  Tool,
} from "@modelcontextprotocol/sdk/types.js";

// Reference data embedded in server
const REFERENCE_DATA = {
  "mucha_artworks": [
    {
      "title": "Amazon.com: onthewall Art nouveau Poster Art Print by Alphonse Mucha Moet:  Posters & Prints",
      "image_url": "https://m.media-amazon.com/images/I/91I8tKKahkL._AC_UF894,1000_QL80_.jpg",
      "thumbnail": "https://serpapi.com/searches/69942001d99612883aa4825c/images/crXVOYyyTCcv8DmPVbXCxLo9lWSX2CnhtlZC-JfOS0I.jpeg",
      "source": "Amazon.com",
      "dimensions": "750x1000"
    },
    {
      "title": "Amazon.com: Alphonse Mucha Vintage posters - Emerald Green Art Nouveau Art  Poster | Nature People Illustrations Fine Art Decorative Art Deco Decor | Art  Nouveau Decor Famous & Cool Vintage Poster (12",
      "image_url": "https://m.media-amazon.com/images/I/917ZeL19k9L._AC_UF894,1000_QL80_.jpg",
      "thumbnail": "https://serpapi.com/searches/69942001d99612883aa4825c/images/wzlPi9VIwD45sE1Vrp6TbvkUr8skSZOU0r5zrSyIxG0.jpeg",
      "source": "Amazon.com",
      "dimensions": "358x1000"
    },
    {
      "title": "8.5x11 Vintage Alphonse Mucha 1898 Fine Art Nouveau Print Picture Poster  Women",
      "image_url": "https://i.ebayimg.com/images/g/KZsAAOSwrxBfeHF4/s-l1200.jpg",
      "thumbnail": "https://serpapi.com/searches/69942001d99612883aa4825c/images/SkgBldX0JR4M6a_oPB-k6M_k_UhcAEJs2_NDAnCUbuQ.jpeg",
      "source": "eBay\u00a0\u00b7\u00a0In stock",
      "dimensions": "927x1200"
    },
    {
      "title": "L'Ermitage - Alphonse Mucha - Art Nouveau Poster Art Print",
      "image_url": "https://render.fineartamerica.com/images/rendered/default/print/5.5/8/break/images/artworkimages/medium/1/lermitage-alphonse-mucha-art-deco-poster-studio-grafiikka.jpg",
      "thumbnail": "https://serpapi.com/searches/69942001d99612883aa4825c/images/of0hfwFn3A0v4dqCC0PT5qhYDf_LdPMOXtVYYkxHf3I.jpeg",
      "source": "Fine Art America\u00a0\u00b7\u00a0In stock",
      "dimensions": "550x800"
    },
    {
      "title": "Art Noveau - Alphonse Mucha \u2013 lovely print on canvas \u2013 Photowall",
      "image_url": "https://images.photowall.com/products/54135/alphonse-mucha-art-noveau.jpg?h=699&q=85",
      "thumbnail": "https://serpapi.com/searches/69942001d99612883aa4825c/images/dU6J65ZE1ecMMXvwduxBV8YMTXDoqJLNpGyq6d9jbcg.jpeg",
      "source": "Photowall",
      "dimensions": "447x699"
    },
    {
      "title": "Alphonse Mucha Print: Art Nouveau Poster 1894. Art Prints, Posters &  Puzzles from Universal Images Group",
      "image_url": "https://www.mediastorehouse.com/p/617/mucha-poster-1894-alphonse-mucha-9786993.jpg.webp",
      "thumbnail": "https://serpapi.com/searches/69942001d99612883aa4825c/images/081EaF0c1aLiGpyvUM9Da-yMSAsKFkKG7GZDZznGL9c.jpeg",
      "source": "Media Storehouse",
      "dimensions": "369x600"
    },
    {
      "title": "Mucha Flowers Lady Art Nouveau Lovely 10x16 Vintage Poster Repro FREE  SHIPPING | eBay",
      "image_url": "https://www.heritageposters.com/mucha166a.jpg",
      "thumbnail": "https://serpapi.com/searches/69942001d99612883aa4825c/images/9fyKwNKlDAn_GD1n9ePDGanLPl7pckmQrFh_Zbzp-7U.jpeg",
      "source": "eBay\u00a0\u00b7\u00a0In stock",
      "dimensions": "500x800"
    },
    {
      "title": "Precious Stones 1902 Mucha Art Nouveau Poster by Vincent Monozlay",
      "image_url": "https://images.fineartamerica.com/images/artworkimages/mediumlarge/3/precious-stones-1902-mucha-art-nouveau-poster-alphonse-mucha.jpg",
      "thumbnail": "https://serpapi.com/searches/69942001d99612883aa4825c/images/5oQzdUd3-I4Z8RA-EaiefWQ08XIRRWBPjnJZKqLtUz8.jpeg",
      "source": "Vincent Monozlay - Pixels",
      "dimensions": "383x900"
    },
    {
      "title": "Amazon.com: Alphonse Mucha Painting Dance Dancer Poster 1898 Bohemian Czech  Painter 1900s Art Nouveau Vintage Cool Wall Art Print Poster 24x36: Posters  & Prints",
      "image_url": "https://m.media-amazon.com/images/I/71FybplDrCS._AC_UF894,1000_QL80_.jpg",
      "thumbnail": "https://serpapi.com/searches/69942001d99612883aa4825c/images/RMqbR1uVtssroHRpomZHK9mRuqqXgih5S2jNQPQ_QJM.jpeg",
      "source": "Amazon.com",
      "dimensions": "667x1000"
    },
    {
      "title": "Alphonse Mucha: Art Nouveau/Nouvelle Femme | Poster House",
      "image_url": "https://posterhouse.org/wp-content/uploads/2019/08/1-Zodiac-by-Alphonse-Mucha-1896-765x1024.jpg",
      "thumbnail": "https://serpapi.com/searches/69942001d99612883aa4825c/images/955yhgs5PLLoj9bmhv-jEnDIsNYgw7X3Uzbk7IufhRg.jpeg",
      "source": "Poster House",
      "dimensions": "765x1024"
    }
  ],
  "architecture_references": [
    {
      "title": "11 Art Nouveau Buildings That'll Make You Fall in Love With the Style |  Architectural Digest",
      "image_url": "https://media.architecturaldigest.com/photos/57ae38a721fff4dc072ead48/master/w_1024%2Cc_limit/art-nouveau-buildings-001.jpg",
      "thumbnail": "https://serpapi.com/searches/699420014de49a1a8d956c68/images/Q09m5kdr1VjZKfOi1IpJBmjQK8TkSyFpJ0wyBLQBPzc.jpeg",
      "source": "Architectural Digest",
      "dimensions": "1024x1536"
    },
    {
      "title": "Art Nouveau Architecture: A Dance of Flowers and Curves",
      "image_url": "https://parametric-architecture.com/wp-content/uploads/2024/10/art-nouveau-architecture-detail.webp",
      "thumbnail": "https://serpapi.com/searches/699420014de49a1a8d956c68/images/EA4CuyIudk7UpT22vvPOsS0Cm0db_SmHci0whaY1TMo.jpeg",
      "source": "Parametric Architecture",
      "dimensions": "1500x1026"
    },
    {
      "title": "5 Incredible Buildings That Embody Art Nouveau Architecture",
      "image_url": "https://mymodernmet.com/wp/wp-content/uploads/2021/02/art-nouveau-buildings-architecture-style-art-nouveau-examples-my-modern-met-0-1.jpg",
      "thumbnail": "https://serpapi.com/searches/699420014de49a1a8d956c68/images/NuY8e9R9mxz97U0s53pt4BOYocmYzZEKgZL73GaO4_U.jpeg",
      "source": "My Modern Met",
      "dimensions": "1200x630"
    },
    {
      "title": "Art Nouveau Architecture: History, Examples, Defining Features",
      "image_url": "https://hips.hearstapps.com/hmg-prod/images/municipal-house-art-nouveau-historical-building-at-royalty-free-image-1732575164.jpg?crop=0.668xw:1.00xh;0.143xw,0&resize=640:*",
      "thumbnail": "https://serpapi.com/searches/699420014de49a1a8d956c68/images/KAa91O6bZc5Ext9yFDva1-rl2FcOsmXiAUsr3oV6icI.jpeg",
      "source": "House Beautiful",
      "dimensions": "640x639"
    },
    {
      "title": "5 Art Nouveau Buildings in England \u2013 The Historic England Blog",
      "image_url": "https://i0.wp.com/heritagecalling.com/wp-content/uploads/2023/08/DP348873.jpg?ssl=1",
      "thumbnail": "https://serpapi.com/searches/699420014de49a1a8d956c68/images/xdGWeSLRebtMZ4vQTmkbThR-HfgY90DGHbvCdApzbq8.jpeg",
      "source": "The Historic England Blog",
      "dimensions": "1145x1500"
    },
    {
      "title": "The Art Nouveau Movement And Its Influence on Architecture | PAACADEMY",
      "image_url": "https://paacademy.com/storage/media/antoni_gaudi_casa_batllo_stirworld_03.webp",
      "thumbnail": "https://serpapi.com/searches/699420014de49a1a8d956c68/images/B3QP_fWOnXVirgrhFFJrJC-VEffWTNUm32uUnyDKOjM.jpeg",
      "source": "PAACADEMY.com",
      "dimensions": "1920x1080"
    },
    {
      "title": "Art Nouveau Architecture \u2014 Madison Trust for Historic Preservation",
      "image_url": "https://images.squarespace-cdn.com/content/v1/5c40df4ef93fd497c87fd880/1622468608373-8GM4LTLBBKK7S6EO2VIU/Screen+Shot+2021-05-31+at+8.42.15+AM.png",
      "thumbnail": "https://serpapi.com/searches/699420014de49a1a8d956c68/images/cQLo_SGJyGACTteLecG6INfYaktopQzpO4J-X_83hwM.jpeg",
      "source": "Madison Trust for Historic Preservation",
      "dimensions": "652x1090"
    },
    {
      "title": "Art Nouveau Architecture -",
      "image_url": "https://www.inspiredspaces.com.au/wp-content/uploads/architectural-details-9-1515029-638x478-1.jpg",
      "thumbnail": "https://serpapi.com/searches/699420014de49a1a8d956c68/images/toi0ZPv8TYjmnV1fxy2pyz69jZgJtppP09UFkFmsuiM.jpeg",
      "source": "Inspired Spaces",
      "dimensions": "638x478"
    },
    {
      "title": "Art Nouveau Architecture: A Dance of Flowers and Curves",
      "image_url": "https://parametric-architecture.com/wp-content/uploads/2024/10/art-nouveau-buildings.webp",
      "thumbnail": "https://serpapi.com/searches/699420014de49a1a8d956c68/images/L-s2s_5_rBV3Iu5QFUay-Neshb6jzUqyIBTN9PlwhHg.jpeg",
      "source": "Parametric Architecture",
      "dimensions": "1500x1000"
    },
    {
      "title": "Art nouveau architecture: features & famous examples | G-Pulse",
      "image_url": "https://partner.gira.de/abbildungen/gira-magazin-jugendstil-architektur-getty-hero-2_26353_1691408424.webp",
      "thumbnail": "https://serpapi.com/searches/699420014de49a1a8d956c68/images/kyvPqaOmoelHbICvBsbdtj9RboCvpxHg2EIUqgSMCo8.jpeg",
      "source": "Gira",
      "dimensions": "1000x600"
    }
  ],
  "style_characteristics": "Alphonse Mucha's style, characteristic of the Art Nouveau movement, is renowned for its distinctive visual elements. His work often features elegant, flowing lines and intricate decorative patterns that emphasize organic forms inspired by nature. Key elements include stylized floral motifs, curvilinear shapes, and a harmonious integration of figures with ornamental backgrounds, creating a sense of movement and fluidity ([Artsy](https://www.artsy.net/article/artsy-editorial-alphonse-muchas-iconic-posters-define-art-nouveau)). Mucha's posters and illustrations frequently depict women with elongated, graceful figures, often surrounded by elaborate halos or floral motifs, emphasizing femininity and beauty ([Britannica](https://britannica.com/art/Art-Nouveau); [Britannica](https://www.britannica.com/biography/Alphonse-Mucha)). His use of rich, pastel color palettes and decorative borders further enhances the ornamental quality typical of Art Nouveau visual elements. Overall, Mucha's style is characterized by its decorative elegance, organic motifs, and a seamless blend of figure and ornamentation that define the aesthetic of the movement ([Wikimedia](https://en.wikisource.org/wiki/An_introduction_to_the_work_of_Alfons_Mucha_and_Art_Nouveau)).",
  "citations": [
    {
      "author": "Cath Pound",
      "favicon": "https://d1s2w0upia4e9w.cloudfront.net/images/favicon.ico",
      "id": "https://artsy.net/article/artsy-editorial-alphonse-muchas-iconic-posters-define-art-nouveau",
      "image": "https://d7hftxdivxxvm.cloudfront.net?height=630&quality=80&resize_to=fill&src=https%3A%2F%2Fartsy-media-uploads.s3.amazonaws.com%2Fd13LvDA1oJEoUeWvHKIauQ%252Fcustom-Custom_Size___ReI%25CC%2580%25C2%2582verie.jpg&width=1200",
      "publishedDate": "2018-11-13T00:00:00.000Z",
      "title": "How Alphonse Mucha\u2019s Iconic Posters Came to Define Art Nouveau",
      "url": "https://www.artsy.net/article/artsy-editorial-alphonse-muchas-iconic-posters-define-art-nouveau"
    },
    {
      "favicon": "https://britannica.com/favicon.png",
      "id": "https://britannica.com/art/Art-Nouveau",
      "publishedDate": "2025-08-22T00:00:00.000Z",
      "title": "Art Nouveau",
      "url": "https://britannica.com/art/Art-Nouveau"
    },
    {
      "favicon": "https://www.britannica.com/favicon.png",
      "id": "https://britannica.com/biography/Alphonse-Mucha",
      "image": "https://cdn.britannica.com/mendel/eb-logo/MendelNewThistleLogo.png",
      "publishedDate": "1998-07-20T00:00:00.000Z",
      "title": "Alphonse Mucha",
      "url": "https://www.britannica.com/biography/Alphonse-Mucha"
    },
    {
      "favicon": "https://en.wikisource.org/static/favicon/wikisource.ico",
      "id": "https://en.wikisource.org/wiki/An_introduction_to_the_work_of_Alfons_Mucha_and_Art_Nouveau",
      "publishedDate": "2020-12-07T00:00:00.000Z",
      "title": "",
      "url": "https://en.wikisource.org/wiki/An_introduction_to_the_work_of_Alfons_Mucha_and_Art_Nouveau"
    },
    {
      "author": "~2025-42119-51",
      "favicon": "https://en.wikipedia.org/static/apple-touch/wikipedia.png",
      "id": "https://en.wikipedia.org/wiki/Alphonse_Mucha",
      "image": "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a4/Alfons_Mucha_in_Studio_%28c._1899%29.jpg/924px-Alfons_Mucha_in_Studio_%28c._1899%29.jpg",
      "title": "Alphonse Mucha",
      "url": "https://en.wikipedia.org/wiki/Alphonse_Mucha"
    },
    {
      "author": "Marie V\u00edtkov\u00e1\n   \n     (opens in new window)\n      ( National Museum in Prague)",
      "favicon": "https://www.europeana.eu/_nuxt/cf61259352b5abdaf73759aae8e8a819.ico",
      "id": "https://europeana.eu/en/blog/alphonse-mucha-master-of-art-nouveau",
      "image": "https://images.ctfassets.net/i01duvb6kq77/415b6aa210269021848eb17ffc99f486/3ea66bbc3b43efb9c64a0b9ac8057ece/Mucha_hero_crop.jpg?w=1200&h=630&fit=fill&f=face&fm=webp&q=40",
      "publishedDate": "2017-04-03T00:00:00.000Z",
      "title": "Alphonse Mucha, master of Art Nouveau",
      "url": "https://www.europeana.eu/en/blog/alphonse-mucha-master-of-art-nouveau"
    },
    {
      "author": "Mucha Foundation",
      "favicon": "https://www.muchafoundation.org/static/img/mf_favicon.png",
      "id": "https://muchafoundation.org/gallery/mucha-at-a-glance-46",
      "image": "https://www.muchafoundation.org/static/img/mucha_logo.png",
      "title": "Mucha Foundation",
      "url": "https://www.muchafoundation.org/gallery/mucha-at-a-glance-46"
    },
    {
      "favicon": "https://prague.org/wp-content/uploads/2022/05/cropped-prague-favicon-32x32.webp",
      "id": "https://prague.org/czech-artist",
      "image": "https://prague.org/wp-content/uploads/2023/06/alphonse-mucha-vintage-art-1576461805pyz-1.jpg",
      "publishedDate": "2023-06-27T00:00:00.000Z",
      "title": "Alphonse Mucha: The Czech Master of Art Nouveau | Prague.org",
      "url": "https://prague.org/czech-artist"
    }
  ]
};

// Create server instance
const server = new Server(
  {
    name: "art-nouveau-anchoring",
    version: "1.0.0",
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// Define available tools
const TOOLS: Tool[] = [
  {
    name: "get_mucha_references",
    description:
      "Get reference images of Alphonse Mucha's Art Nouveau artwork. " +
      "Returns high-quality image URLs with descriptions for use as visual anchors. " +
      "Mucha's style features flowing lines, ornate decorative patterns, " +
      "feminine figures with elaborate floral motifs, and muted pastel colors.",
    inputSchema: {
      type: "object",
      properties: {
        count: {
          type: "number",
          description: "Number of reference images to return (1-10)",
          default: 5,
          minimum: 1,
          maximum: 10,
        },
      },
    },
  },
  {
    name: "get_architecture_references",
    description:
      "Get reference images of Art Nouveau architecture. " +
      "Returns images of iconic Art Nouveau buildings and architectural details " +
      "featuring organic forms, curved lines, and nature-inspired decorations. " +
      "Ideal for understanding structural elements, facades, and ornamental details.",
    inputSchema: {
      type: "object",
      properties: {
        count: {
          type: "number",
          description: "Number of reference images to return (1-10)",
          default: 5,
          minimum: 1,
          maximum: 10,
        },
      },
    },
  },
  {
    name: "get_style_characteristics",
    description:
      "Get detailed description of Art Nouveau style characteristics. " +
      "Returns comprehensive information about visual elements, techniques, " +
      "color palettes, and design principles used in Art Nouveau art and architecture.",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
  {
    name: "get_combined_anchors",
    description:
      "Get a comprehensive set of visual anchors combining both Mucha artworks " +
      "and architectural references. Ideal for projects that need full Art Nouveau " +
      "context with both artistic and architectural elements.",
    inputSchema: {
      type: "object",
      properties: {
        mucha_count: {
          type: "number",
          description: "Number of Mucha artwork references (1-10)",
          default: 3,
          minimum: 1,
          maximum: 10,
        },
        architecture_count: {
          type: "number",
          description: "Number of architecture references (1-10)",
          default: 3,
          minimum: 1,
          maximum: 10,
        },
      },
    },
  },
  {
    name: "create_prompt_with_anchors",
    description:
      "Generate an image generation prompt enhanced with Art Nouveau visual anchors. " +
      "Takes a base prompt and enriches it with specific Art Nouveau characteristics " +
      "and reference descriptions to guide the generation toward authentic style.",
    inputSchema: {
      type: "object",
      properties: {
        base_prompt: {
          type: "string",
          description: "The base description of what to generate",
        },
        include_mucha: {
          type: "boolean",
          description: "Include Mucha-style characteristics",
          default: true,
        },
        include_architecture: {
          type: "boolean",
          description: "Include architectural elements",
          default: false,
        },
      },
      required: ["base_prompt"],
    },
  },
];

// List tools handler
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: TOOLS,
  };
});

// Call tool handler
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  switch (name) {
    case "get_mucha_references": {
      const count = Math.min(
        typeof args?.count === "number" ? args.count : 5,
        10
      );
      const references = REFERENCE_DATA.mucha_artworks.slice(0, count);

      let response = `# Alphonse Mucha Art Nouveau References (${count} images)\n\n`;
      response += "## Style Overview\n";
      response += "Mucha's signature Art Nouveau style features:\n";
      response += "- Elegant flowing lines and curves\n";
      response += "- Ornate decorative borders and frames\n";
      response += "- Feminine figures with elaborate floral motifs\n";
      response += "- Muted pastel color palettes (soft pinks, blues, greens)\n";
      response += "- Byzantine and medieval influences\n\n";
      response += "## Reference Images\n\n";

      references.forEach((ref: any, i: number) => {
        response += `### ${i + 1}. ${ref.title}\n`;
        response += `- **Image URL**: ${ref.image_url}\n`;
        response += `- **Dimensions**: ${ref.dimensions}\n`;
        response += `- **Source**: ${ref.source}\n\n`;
      });

      return {
        content: [
          {
            type: "text",
            text: response,
          },
        ],
      };
    }

    case "get_architecture_references": {
      const count = Math.min(
        typeof args?.count === "number" ? args.count : 5,
        10
      );
      const references = REFERENCE_DATA.architecture_references.slice(0, count);

      let response = `# Art Nouveau Architecture References (${count} images)\n\n`;
      response += "## Architectural Characteristics\n";
      response += "Art Nouveau architecture features:\n";
      response += "- Organic, flowing forms inspired by nature\n";
      response += "- Asymmetrical facades and layouts\n";
      response += "- Curved lines and whiplash motifs\n";
      response += "- Decorative ironwork with plant motifs\n";
      response += "- Stained glass and mosaic details\n";
      response += "- Integration of art and structure\n\n";
      response += "## Reference Images\n\n";

      references.forEach((ref: any, i: number) => {
        response += `### ${i + 1}. ${ref.title}\n`;
        response += `- **Image URL**: ${ref.image_url}\n`;
        response += `- **Dimensions**: ${ref.dimensions}\n`;
        response += `- **Source**: ${ref.source}\n\n`;
      });

      return {
        content: [
          {
            type: "text",
            text: response,
          },
        ],
      };
    }

    case "get_style_characteristics": {
      let response = "# Art Nouveau Style Characteristics\n\n";
      response += "## Overview\n";
      response += REFERENCE_DATA.style_characteristics + "\n\n";
      response += "## Key Visual Elements\n\n";
      response += "### Line Work\n";
      response += "- Long, sinuous lines (whiplash curves)\n";
      response += "- Asymmetrical compositions\n";
      response += "- Flowing, organic forms\n\n";
      response += "### Motifs\n";
      response += "- Flowers (especially lilies, irises, poppies)\n";
      response += "- Vines and tendrils\n";
      response += "- Peacock feathers\n";
      response += "- Feminine figures with flowing hair\n\n";
      response += "### Color Palette\n";
      response += "- Muted pastels\n";
      response += "- Gold and bronze accents\n";
      response += "- Soft greens, blues, pinks\n";
      response += "- Earth tones\n\n";
      response += "## Sources\n\n";

      REFERENCE_DATA.citations.forEach((citation: any) => {
        response += `- [${citation.title}](${citation.url})\n`;
      });

      return {
        content: [
          {
            type: "text",
            text: response,
          },
        ],
      };
    }

    case "get_combined_anchors": {
      const muchaCount = Math.min(
        typeof args?.mucha_count === "number" ? args.mucha_count : 3,
        10
      );
      const archCount = Math.min(
        typeof args?.architecture_count === "number" ? args.architecture_count : 3,
        10
      );

      const muchaRefs = REFERENCE_DATA.mucha_artworks.slice(0, muchaCount);
      const archRefs = REFERENCE_DATA.architecture_references.slice(0, archCount);

      let response = "# Complete Art Nouveau Visual Anchor Set\n\n";
      response += "## Alphonse Mucha Artworks\n\n";

      muchaRefs.forEach((ref: any, i: number) => {
        response += `### Mucha ${i + 1}: ${ref.title}\n`;
        response += `![Mucha Reference](${ref.image_url})\n`;
        response += `- Dimensions: ${ref.dimensions}\n\n`;
      });

      response += "\n## Art Nouveau Architecture\n\n";

      archRefs.forEach((ref: any, i: number) => {
        response += `### Architecture ${i + 1}: ${ref.title}\n`;
        response += `![Architecture Reference](${ref.image_url})\n`;
        response += `- Dimensions: ${ref.dimensions}\n\n`;
      });

      return {
        content: [
          {
            type: "text",
            text: response,
          },
        ],
      };
    }

    case "create_prompt_with_anchors": {
      if (!args?.base_prompt) {
        throw new Error("base_prompt is required");
      }

      const basePrompt = args.base_prompt as string;
      const includeMucha = args.include_mucha !== false;
      const includeArch = args.include_architecture === true;

      let enhancedPrompt = `${basePrompt}, in Art Nouveau style`;

      if (includeMucha) {
        enhancedPrompt +=
          ", inspired by Alphonse Mucha's elegant flowing lines, " +
          "ornate decorative borders with floral motifs, feminine grace, " +
          "muted pastel color palette (soft pinks, blues, golds), " +
          "intricate patterns with Byzantine influences";
      }

      if (includeArch) {
        enhancedPrompt +=
          ", incorporating Art Nouveau architectural elements: " +
          "organic flowing forms, curved asymmetrical lines, " +
          "decorative ironwork with plant motifs, nature-inspired ornamental details";
      }

      let response = "# Enhanced Prompt with Art Nouveau Anchors\n\n";
      response += "## Original Prompt\n";
      response += `${basePrompt}\n\n`;
      response += "## Enhanced Prompt\n";
      response += `${enhancedPrompt}\n\n`;
      response += "## Visual References to Consider\n\n";

      if (includeMucha) {
        response += "### Mucha Style Elements\n";
        REFERENCE_DATA.mucha_artworks.slice(0, 3).forEach((ref: any) => {
          response += `- [${ref.title}](${ref.image_url})\n`;
        });
        response += "\n";
      }

      if (includeArch) {
        response += "### Architectural Elements\n";
        REFERENCE_DATA.architecture_references.slice(0, 3).forEach((ref: any) => {
          response += `- [${ref.title}](${ref.image_url})\n`;
        });
      }

      return {
        content: [
          {
            type: "text",
            text: response,
          },
        ],
      };
    }

    default:
      throw new Error(`Unknown tool: ${name}`);
  }
});

// Start the server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Art Nouveau MCP Server running on stdio");
}

main().catch((error) => {
  console.error("Server error:", error);
  process.exit(1);
});
