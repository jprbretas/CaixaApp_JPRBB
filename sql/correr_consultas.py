"""
Corre as consultas da Tarefa 4 (sql/Tarefa4_Consultas.sql) sobre a caixa.db e mostra os resultados
em tabelas, com o nome das colunas.

Abre a base de dados só para leitura: não altera nada, mesmo com a aplicação a correr.

Uso, no terminal, na pasta da solução:
    python sql/correr_consultas.py                       as 5 consultas
    python sql/correr_consultas.py 3                     só a consulta 3
    python sql/correr_consultas.py "SELECT * FROM Clientes"   uma consulta qualquer (só leitura)
"""
import re
import sqlite3
import sys
from pathlib import Path

PASTA = Path(__file__).resolve().parent
FICHEIRO_SQL = PASTA / "Tarefa4_Consultas.sql"
BASE_DADOS = PASTA.parent / "CaixaProjeto.ApiService" / "caixa.db"


def ler_consultas():
    """Devolve a lista de (título, sql) do ficheiro, pela ordem em que aparecem."""
    texto = FICHEIRO_SQL.read_text(encoding="utf-8")

    # Os títulos são as linhas "-- 1. Número de pedidos ...", "-- 2. ...", etc.
    titulos = re.findall(r"^--\s*(\d+\.\s+.+)$", texto, re.MULTILINE)

    # Tira as linhas de comentário (que também podem ter ";") e separa o resto pelos ";"
    linhas = [linha for linha in texto.splitlines() if not linha.strip().startswith("--")]
    consultas = [c.strip() for c in "\n".join(linhas).split(";") if c.strip()]

    return list(zip(titulos, consultas))


def mostrar_tabela(cursor):
    """Escreve as linhas do resultado numa tabela alinhada, com o nome das colunas por cima."""
    colunas = [descricao[0] for descricao in cursor.description]
    linhas = [["" if valor is None else str(valor) for valor in linha] for linha in cursor.fetchall()]

    if not linhas:
        print("  (sem resultados)")
        return

    larguras = [len(nome) for nome in colunas]
    for linha in linhas:
        for i, valor in enumerate(linha):
            larguras[i] = max(larguras[i], len(valor))

    print("  " + " | ".join(nome.ljust(larguras[i]) for i, nome in enumerate(colunas)))
    print("  " + "-+-".join("-" * largura for largura in larguras))
    for linha in linhas:
        print("  " + " | ".join(valor.ljust(larguras[i]) for i, valor in enumerate(linha)))


def main():
    if not BASE_DADOS.exists():
        print(f"Não encontrei a base de dados em {BASE_DADOS}.")
        print("Arranque a aplicação uma vez (F5 no AppHost) para ela ser criada.")
        return

    # "mode=ro": só leitura. Um UPDATE ou DELETE dá erro em vez de alterar os dados.
    ligacao = sqlite3.connect(BASE_DADOS.as_uri() + "?mode=ro", uri=True)

    argumento = sys.argv[1] if len(sys.argv) > 1 else None

    if argumento is not None and not argumento.isdigit():
        # Uma consulta escrita no próprio comando
        print(f"\n{argumento}\n")
        try:
            mostrar_tabela(ligacao.execute(argumento))
        except sqlite3.Error as erro:
            print(f"  O SQLite recusou a consulta: {erro}")
            if "readonly" in str(erro):
                print("  Este script só lê a base de dados: UPDATE, INSERT e DELETE não são permitidos.")
        return

    consultas = ler_consultas()
    for numero, (titulo, sql) in enumerate(consultas, start=1):
        if argumento is not None and int(argumento) != numero:
            continue
        print(f"\n{titulo}\n")
        mostrar_tabela(ligacao.execute(sql))
    print()


if __name__ == "__main__":
    main()
